using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalState;
using Zephyr.GOAP.Debugger;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System.GoalManage;
using Zephyr.GOAP.System.GoapPlanningJob;
using Zephyr.GOAP.System.SensorManage;

namespace Zephyr.GOAP.System
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(SensorSystemGroup))]
    [UpdateAfter(typeof(AgentGoalMonitorSystemGroup))]
    public abstract class GoalPlanningSystemBase : ComponentSystem
    {
        /// <summary>
        /// 对goal展开的层数上限
        /// </summary>
        public int ExpandIterations = 100;

        public int PathFindingIterations = 1000;

        /// <summary>
        /// 生成路径的步数上限
        /// </summary>
        public int PathNodeLimit = 1000;

        /// <summary>
        /// 参与一次planning的agent的上限，以避免node graph过度膨胀
        /// </summary>
        public int AgentAmountForPlanning = 3;

        private EntityQuery _allAgentQuery, _idleAgentQuery, _goalQuery, _deltaQuery;

        public IGoapDebugger Debugger;

        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            ECBSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            _allAgentQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new []
                {
                    ComponentType.ReadOnly<Agent>(),
                }
            });
            _idleAgentQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new []
                {
                    ComponentType.ReadOnly<Agent>(),
                    ComponentType.ReadOnly<Translation>(), 
                    ComponentType.ReadOnly<AgentMoveSpeed>(), 
                    ComponentType.ReadOnly<Idle>(), 
                }
            });
            _goalQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<Goal>(), ComponentType.ReadOnly<IdleGoal>(), },
            });
            _deltaQuery = GetEntityQuery(ComponentType.ReadOnly<DeltaStates>(),
                ComponentType.ReadOnly<State>());
        }

        protected override void OnUpdate()
        {
            //如果没有空闲agent或者没有任务,不运行
            var idleAgents = _idleAgentQuery.ToEntityArray(Allocator.TempJob);
            if (idleAgents.Length <= 0 || _goalQuery.CalculateEntityCount() <= 0)
            {
                idleAgents.Dispose();
                return;
            }
            
            //找到最急需的一个goal
            var goal = GetMostPriorityGoal();
            var goalRequires = new StateGroup(1, Allocator.TempJob) {goal.Require};
            
            var allAgents = _allAgentQuery.ToEntityArray(Allocator.TempJob);
            
            //如果goal有指定agent，但是这个agent在忙，不运行
            var target = goalRequires[0].Target;
            if (allAgents.Contains(target) && !idleAgents.Contains(target))
            {
                idleAgents.Dispose();
                goalRequires.Dispose();
                allAgents.Dispose();
                return;
            }
            allAgents.Dispose();

            //缩减参与plan的agent数量到优化上限
            var planningAgents = TrimAgents(idleAgents, goal, Allocator.TempJob);
            var agentAmount = planningAgents.Length;
            idleAgents.Dispose();

            var agentTranslations = _idleAgentQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            var agentMoveSpeeds = _idleAgentQuery.ToComponentDataArray<AgentMoveSpeed>(Allocator.TempJob);
            var agentStartTimes = new NativeArray<float>(agentTranslations.Length, Allocator.TempJob);
            
            //从baseState的存储Entity上拿取base states，减去delta states，以得到实际的base states
            var baseStates = CalcBaseStates();
            
            //组织StackData
            var stackData = new StackData(planningAgents, agentTranslations, baseStates);
            agentTranslations.Dispose();

            Debugger?.StartLog(EntityManager);
            Debugger?.SetBaseStates(stackData.BaseStates, EntityManager);

            var uncheckedNodes = new NativeHashMap<int, Node>(32*agentAmount, Allocator.TempJob);
            var uncheckedNodesWriter = uncheckedNodes.AsParallelWriter();
            var unexpandedNodes = new NativeList<Node>(Allocator.TempJob);
            var expandedNodes = new NativeList<Node>(Allocator.TempJob);

            var nodeGraph = new NodeGraph(128*agentAmount, baseStates, Allocator.TempJob);

            var goalPrecondition = new StateGroup();
            var goalEffects = new StateGroup();
            var goalDeltas = new StateGroup();
            var goalNode = new Node(goalPrecondition, goalEffects, goalRequires, goalDeltas,
                "goal", 0, 0, 0, Entity.Null);
            
            //goalNode进入graph
            nodeGraph.SetGoalNode(goalNode, goalRequires);

            //goalNode进入待检查列表
            uncheckedNodes.Add(goalNode.HashCode, goalNode);

            var iteration = 1; //goal node iteration is 0
            var foundPlan = false;
            
            while (uncheckedNodes.Count() > 0 && iteration < ExpandIterations)
            {
                //对待检查列表进行检查（与BaseStates比对）
                if (CheckNodes(uncheckedNodes, nodeGraph, stackData.BaseStates,
                    unexpandedNodes, iteration)) foundPlan = true;
                

                //对待展开列表进行展开，并挑选进入待检查和展开后列表
                ExpandNodes(unexpandedNodes, stackData, nodeGraph,
                    uncheckedNodesWriter, expandedNodes, iteration);

                //直至待展开列表为空或Early Exit
                iteration++;
            }

            var nodes = nodeGraph.GetNodes(Allocator.Temp);
            Debugger?.Log($"{nodes.Length} nodes in graph");
            nodes.Dispose();

            Debugger?.SetPlanSuccess(foundPlan);
            
            if (!foundPlan)
            {
                //在展开阶段没有能够链接到current state的话，就没有找到规划，也就不用继续寻路了
                //目前对于规划失败的情况，就直接转入NoGoal状态
                var reason = "";
                if (uncheckedNodes.Count() <= 0) reason = "No more nodes";
                else if (iteration >= ExpandIterations) reason = "Max iteration reached";
                Debugger?.LogWarning($"goal plan failed : {reason}");
                
                Utils.NextGoalState<IdleGoal, PlanFailedGoal>(goal.GoalEntity,
                    EntityManager, Time.ElapsedTime);

                var buffer = EntityManager.AddBuffer<FailedPlanLog>(goal.GoalEntity);
                buffer.Add(new FailedPlanLog {Time = (float)Time.ElapsedTime});
                
                Debugger?.SetNodeGraph(nodeGraph, EntityManager);
            }
            else
            {
                //寻路
                var nodeAgentInfos = new NativeMultiHashMap<int, NodeAgentInfo>(nodeGraph.Length(), Allocator.TempJob);
                var nodeTotalTimes = new NativeHashMap<int, float>(nodeGraph.Length(), Allocator.TempJob);
                var pathNodesEstimateNavigateTime = new NativeHashMap<int, float>(nodeGraph.Length(), Allocator.TempJob);
                var pathNodeNavigateSubjects = new NativeHashMap<int, Entity>(nodeGraph.Length(), Allocator.TempJob);
                var pathNodeSpecifiedPreconditionIndices = new NativeList<int>(Allocator.TempJob);
                var pathNodeSpecifiedPreconditions = new NativeList<State>(Allocator.TempJob);
                var rewardSum = new NativeHashMap<int, float>(nodeGraph.Length(), Allocator.TempJob);
                
                var pathNodes = FindPath(nodeGraph, stackData,
                    agentMoveSpeeds, agentStartTimes, nodeAgentInfos, nodeTotalTimes, pathNodesEstimateNavigateTime,
                    pathNodeNavigateSubjects, pathNodeSpecifiedPreconditionIndices, pathNodeSpecifiedPreconditions,
                    rewardSum);
                
                SavePath(pathNodes, nodeGraph, pathNodesEstimateNavigateTime, 
                    pathNodeNavigateSubjects, pathNodeSpecifiedPreconditionIndices, pathNodeSpecifiedPreconditions,
                    goal.GoalEntity, out var pathEntities);
                
                //保存总预测时间
                var totalTime = nodeTotalTimes[pathNodes[0].HashCode];
                goal.EstimatePeriod = totalTime;
                EntityManager.SetComponentData(goal.GoalEntity, goal);

                Debugger?.SetNodeGraph(nodeGraph, EntityManager);
                Debugger?.SetNodeAgentInfos(EntityManager, nodeAgentInfos);
                Debugger?.SetNodeTotalTimes(nodeTotalTimes);
                Debugger?.SetPathResult(EntityManager, pathEntities, pathNodes);
                Debugger?.SetSpecifiedPreconditions(EntityManager,
                    pathNodeSpecifiedPreconditionIndices, pathNodeSpecifiedPreconditions);
                Debugger?.SetRewardSum(rewardSum);

                nodeAgentInfos.Dispose();
                pathNodes.Dispose();
                pathEntities.Dispose();
                nodeTotalTimes.Dispose();
                pathNodesEstimateNavigateTime.Dispose();
                pathNodeNavigateSubjects.Dispose();
                pathNodeSpecifiedPreconditionIndices.Dispose();
                pathNodeSpecifiedPreconditions.Dispose();
                rewardSum.Dispose();

                Utils.NextGoalState<IdleGoal, ExecutingGoal>(goal.GoalEntity,
                    EntityManager, Time.ElapsedTime);
            }
            
            Debugger?.LogDone();

            uncheckedNodes.Dispose();
            unexpandedNodes.Dispose();
            expandedNodes.Dispose();
            nodeGraph.Dispose();
            
            goalRequires.Dispose();
            agentMoveSpeeds.Dispose();
            agentStartTimes.Dispose();
            stackData.Dispose();
            planningAgents.Dispose();
        }

        private StateGroup CalcBaseStates()
        {
            var baseStateBuffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            var result = new StateGroup(baseStateBuffer, Allocator.TempJob);
            var deltas = new StateGroup(4, Allocator.Temp);
            var deltaEntities = _deltaQuery.ToEntityArray(Allocator.TempJob);
            for (var deltaId = 0; deltaId < deltaEntities.Length; deltaId++)
            {
                var deltaEntity = deltaEntities[deltaId];
                var states = EntityManager.GetBuffer<State>(deltaEntity);
                for (var stateId = 0; stateId < states.Length; stateId++)
                {
                    deltas.Add(states[stateId]);
                }
            }

            deltaEntities.Dispose();
            result.MINUS(deltas);
            return result;
        }

        /// <summary>
        /// 获取最优先goal
        /// </summary>
        /// <returns></returns>
        private Goal GetMostPriorityGoal()
        {
            var goals = _goalQuery.ToComponentDataArray<Goal>(Allocator.TempJob);
            var sortedGoals = new NativeMinHeap<Goal>(100, Allocator.TempJob);
            foreach (var goal in goals)
            {
                var priority = CalcGoalPriority(goal.Priority, goal.CreateTime);
                sortedGoals.Push(new MinHeapNode<Goal>(goal, priority));
            }

            var minNode = sortedGoals[sortedGoals.Pop()];

            sortedGoals.Dispose();
            goals.Dispose();

            return minNode.Content;
        }

        private NativeList<Entity> TrimAgents(NativeArray<Entity> idleAgents, Goal goal, Allocator allocator)
        {
            var agentP = 0;
            var workAgents = new NativeList<Entity>(allocator);

            //先把目标agent加入
            var target = goal.Require.Target;
            if (idleAgents.Contains(target))
            {
                workAgents.Add(target);
                agentP++;
            }

            //再加入其它agent，直到达到上限
            for (var agentId = 0; agentId < idleAgents.Length; agentId++)
            {
                if (agentP >= AgentAmountForPlanning) break;
                var agentEntity = idleAgents[agentId];
                if (agentEntity.Equals(target)) continue;
                workAgents.Add(agentEntity);
                agentP++;
            }

            return workAgents;
        }

        /// <summary>
        /// 基于goal的priority和createTime为goal计算排序时的优先级
        /// todo 正式项目应开放编辑具体公式以方便修改
        /// <param name="priority"></param>
        /// <param name="createTime"></param>
        /// <returns></returns>
        /// </summary>
        private float CalcGoalPriority(Priority priority, double createTime)
        { 
            return (float) (Priority.Max - priority + createTime / 60);
        }
        
        private NativeList<Node> FindPath(NodeGraph nodeGraph, StackData stackData,
            NativeArray<AgentMoveSpeed> agentMoveSpeed, NativeArray<float> agentStartTime,
            NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos,
            NativeHashMap<int, float> nodeTotalTimes, NativeHashMap<int, float> nodeNavigateStartTimes,
            NativeHashMap<int, Entity> nodeNavigateSubjects,
            NativeList<int> pathNodeSpecifiedPreconditionIndices,
            NativeList<State> pathNodeSpecifiedPreconditions,
            NativeHashMap<int, float> rewardSum)
        {
            var pathResult = new NativeList<Node>(Allocator.TempJob);
            var pathFindingJob = new PathFindingJob
            {
                StartNodeId = nodeGraph.GetStartNode().GetHashCode(),
                GoalNodeId = nodeGraph.GetGoalNode().GetHashCode(),
                IterationLimit = PathFindingIterations,
                NodeGraph = nodeGraph,
                AgentEntities =  stackData.AgentEntities,
                AgentStartPositions = stackData.AgentPositions,
                AgentMoveSpeeds = agentMoveSpeed,
                AgentStartTime = agentStartTime,
                PathNodeLimit = PathNodeLimit,
                Result = pathResult,
                NodeAgentInfos = nodeAgentInfos,
                NodeTotalTimes = nodeTotalTimes,
                NodesEstimateNavigateTimeWriter = nodeNavigateStartTimes.AsParallelWriter(),
                NodeNavigateSubjects = nodeNavigateSubjects,
                SpecifiedPreconditionIndices = pathNodeSpecifiedPreconditionIndices,
                SpecifiedPreconditions = pathNodeSpecifiedPreconditions,
                RewardSum = rewardSum
            };
            var handle = pathFindingJob.Schedule();
            handle.Complete();

            return pathResult;
        }

        private void SavePath([ReadOnly]NativeList<Node> pathNodes, [ReadOnly]NodeGraph nodeGraph,
            [ReadOnly]NativeHashMap<int, float> pathNodesEstimateNavigateTime,
            [ReadOnly]NativeHashMap<int, Entity> pathNodeNavigateSubjects,
            [ReadOnly]NativeList<int> pathNodeSpecifiedPreconditionIndices,
            [ReadOnly]NativeList<State> pathNodeSpecifiedPreconditions, Entity goalEntity,
            out NativeArray<Entity> pathEntities)
        {
            pathEntities = new NativeArray<Entity>(pathNodes.Length, Allocator.Temp);
            var pathPreconditionHashes = new NativeList<ZephyrValueTuple<int, int>>(Allocator.Temp);
            for (var i = 0; i < pathNodes.Length; i++)
            {
                var node = pathNodes[i];
                var effects = nodeGraph.GetEffects(node, Allocator.Temp);

                var entity = EntityManager.CreateEntity();
                pathEntities[i] = entity;
                var preconditions = new StateGroup(1, Allocator.Temp);
                // add states & dependencies
                var stateBuffer = EntityManager.AddBuffer<State>(entity);
                //precondition不从NodeGraph来，而是用寻路时得到的明确版本
                for (var j = 0; j < pathNodeSpecifiedPreconditionIndices.Length; j++)
                {
                    var specifiedPrecondition = pathNodeSpecifiedPreconditions[j];
                    preconditions.Add(specifiedPrecondition);
                    if (!pathNodeSpecifiedPreconditionIndices[j].Equals(node.HashCode)) continue;
                    stateBuffer.Add(specifiedPrecondition);
                    node.PreconditionsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
                    pathPreconditionHashes.Add(new ZephyrValueTuple<int,int>(node.HashCode, specifiedPrecondition.GetHashCode()));
                }
                for (var j = 0; j < effects.Length(); j++)
                {
                    stateBuffer.Add(effects[j]);
                    node.EffectsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
                }

                //save estimate start time
                if (pathNodesEstimateNavigateTime.ContainsKey(node.HashCode))
                {
                    node.EstimateStartTime = pathNodesEstimateNavigateTime[node.HashCode];
                }

                if (pathNodeNavigateSubjects.ContainsKey(node.HashCode))
                {
                    node.NavigatingSubject = pathNodeNavigateSubjects[node.HashCode];
                }
                
                //add node
                pathNodes[i] = node;
                EntityManager.AddComponentData(entity, node);
                
                //save delta for base states
                var deltas = nodeGraph.GetDeltas(node, Allocator.Temp);
                var deltasStateGroup = new StateGroup(deltas, Allocator.Temp);
                deltasStateGroup.AND(preconditions);    //filter my deltas
                
                if (deltasStateGroup.Length() > 0)
                {
                    var deltaEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData(deltaEntity,
                        new DeltaStates {ActionNodeEntity = entity});
                    var buffer = EntityManager.AddBuffer<State>(deltaEntity);
                    for (var deltaId = 0; deltaId < deltasStateGroup.Length(); deltaId++)
                    {
                        buffer.Add(deltasStateGroup[deltaId]);
                    }
                }
                deltasStateGroup.Dispose();
                deltas.Dispose();
                
                preconditions.Dispose();
                effects.Dispose();
            }

            //connect dependencies
            for (var thisNodeId = 0; thisNodeId < pathEntities.Length; thisNodeId++)
            {
                var entity = pathEntities[thisNodeId];
                var buffer = EntityManager.AddBuffer<NodeDependency>(entity);
                var node = pathNodes[thisNodeId];
                var nodeHash = node.HashCode;
                

                //遍历所有节点，如果某个节点的某个effect与我的某个precondition一致，那么他是我的一个依赖
                for (var otherNodeId = 0; otherNodeId < pathEntities.Length; otherNodeId++)
                {
                    var otherEntity = pathEntities[otherNodeId];
                    if (otherEntity.Equals(entity)) continue;
                    var otherNode = pathNodes[otherNodeId];
                    var otherNodeStates = EntityManager.GetBuffer<State>(otherEntity);
                    for (var otherStateId = 0; otherStateId < otherNodeStates.Length; otherStateId++)
                    {
                        if ((otherNode.EffectsBitmask & (ulong)1<<otherStateId) <= 0) continue;
                        var otherEffect = otherNodeStates[otherStateId];
                        if (!pathPreconditionHashes.Contains(new ZephyrValueTuple<int,int>(nodeHash, otherEffect.GetHashCode())))
                            continue;
                        buffer.Add(new NodeDependency {Entity = otherEntity});
                    }
                }
            }
            
            //双向链接
            var goalBuffer = EntityManager.AddBuffer<ActionNodeOfGoal>(goalEntity);
            for (var i = 0; i < pathEntities.Length; i++)
            {
                goalBuffer.Add(new ActionNodeOfGoal {ActionNodeEntity = pathEntities[i]});
            }
            //从node到goal的链接只存于起始node，用于通知path开始执行时间
            EntityManager.AddComponentData(pathEntities[pathEntities.Length-1], new GoalRefForNode{GoalEntity = goalEntity});

            pathPreconditionHashes.Dispose();
        }

        /// <summary>
        /// 与BaseStates一致的state被从Node中移除
        /// 出现全部State都被移除的Node时，视为找到Plan，其后追加空Node作为起点，可以考虑此时EarlyExit
        /// 对于还有State不满足的Node进入待展开列表
        /// </summary>
        /// <param name="uncheckedNodes"></param>
        /// <param name="nodeGraph"></param>
        /// <param name="baseStates"></param>
        /// <param name="unexpandedNodes"></param>
        /// <param name="iteration"></param>
        public bool CheckNodes(NativeHashMap<int, Node> uncheckedNodes, NodeGraph nodeGraph,
            StateGroup baseStates, NativeList<Node> unexpandedNodes, int iteration)
        {
            var checkTime = DateTime.Now;
            bool foundPlan = false;
            var nodes = uncheckedNodes.GetValueArray(Allocator.Temp);
            for (var nodeId = 0; nodeId < nodes.Length; nodeId++)
            {
                var uncheckedNode = nodes[nodeId];
                Debugger?.Log("check node: " + uncheckedNode.Name);
                nodeGraph.CleanAllDuplicateStates(uncheckedNode);

                var uncheckedRequires = nodeGraph.GetRequires(uncheckedNode, Allocator.Temp);

                //为了避免没有state的node(例如wander)与startNode有相同的hash，这种node被强制给了一个空state
                //因此在只有1个state且内容为空时，也应视为找到了plan
                if (uncheckedRequires.Length <= 0 ||
                    (uncheckedRequires.Length == 1 && uncheckedRequires[0].Equals(default)))
                {
                    //找到Plan，追加起点Node
                    Debugger?.Log("found plan: " + uncheckedNode.Name);
                    nodeGraph.LinkStartNode(uncheckedNode);
                    foundPlan = true;
                    //todo Early Exit
                }

                //检查uncheckedNodes的parent是否已经存在于其children之中
                //如果出现这种情况说明产生了循环，移去新得到的edge
                //并且不不把此uncheckedNode加入待展开列表
                var loop = false;

                var parents = nodeGraph.GetNodeParents(uncheckedNode.HashCode, Allocator.Temp);


                for (var parentId = 0; parentId < parents.Length; parentId++)
                {
                    var parentHash = parents[parentId];
                    if (RemoveLoop(nodeGraph, uncheckedNode.HashCode, parentHash))
                    {
                        loop = true;
                    }
                }

                if (!loop)
                {
                    //没有产生循环，则把此node置入待展开列表
                    //如果这个node没有state，例如WanderAction
                    //则不需要继续展开了
                    if (uncheckedRequires.Length > 0)
                    {
                        unexpandedNodes.Add(uncheckedNode);
                    }
                }
                else
                {
                    //否则的话此node进入dead end 列表，以供debug查看
                    nodeGraph.AddDeadEndNode(uncheckedNode.HashCode);
                }

                uncheckedRequires.Dispose();
            }

            nodes.Dispose();
            uncheckedNodes.Clear();
            Debugger?.LogPerformance($"checkTime{iteration}: " +
                                     $"{(DateTime.Now - checkTime).TotalMilliseconds:F1}");
                ;
            return foundPlan;
        }

        private static bool RemoveLoop(NodeGraph nodeGraph, int nodeHash, int parentHash)
        {
            var children = nodeGraph.GetChildren(nodeHash, Allocator.Temp);
            for (var childId = 0; childId < children.Length; childId++)
            {
                var childHash = children[childId];
                if (!parentHash.Equals(childHash))
                {
                    RemoveLoop(nodeGraph, childHash, parentHash);
                    continue;   
                }
                nodeGraph.RemoveConnection(nodeHash, parentHash);
                return true;
            }

            return false;
        }

        public void ExpandNodes(NativeList<Node> unexpandedNodes, StackData stackData,
            NodeGraph nodeGraph, NativeHashMap<int, Node>.ParallelWriter uncheckedNodes, NativeList<Node> expandedNodes,
            int iteration)
        {
            if (unexpandedNodes.Length <= 0) return;
            
            var expandStartTime = DateTime.Now;
            
            foreach (var node in unexpandedNodes)
            {
                Debugger?.Log("expanding node: "+node.Name+", "+node.GetHashCode());
            }

            var existedNodesHash = nodeGraph.GetAllNodesHash(Allocator.TempJob);
            var nodesWriter = nodeGraph.NodesWriter;
            var nodeToParentsWriter = nodeGraph.NodeToParentsWriter;

            var statesWriter = nodeGraph.StatesWriter;
            var effectHashesWriter = nodeGraph.EffectHashesWriter;
            var preconditionHashesWriter = nodeGraph.PreconditionHashesWriter;
            var requireHashesWriter = nodeGraph.RequireHashesWriter;
            var deltaHashesWriter = nodeGraph.DeltaHashesWriter;

            var agentAmount = stackData.AgentEntities.Length;
            var nodeAmount = unexpandedNodes.Length;
            var nodeAgentPairAmount = unexpandedNodes.Length * agentAmount;

            var nodeAgentPairs =
                new NativeArray<ZephyrValueTuple<Entity, Node>>(nodeAgentPairAmount, Allocator.TempJob);
            for (var agentId = 0; agentId < stackData.AgentEntities.Length; agentId++)
            {
                for (var nodeId = 0; nodeId < unexpandedNodes.Length; nodeId++)
                {
                    nodeAgentPairs[agentId*nodeAmount + nodeId] =
                        new ZephyrValueTuple<Entity,Node>(stackData.AgentEntities[agentId], unexpandedNodes[nodeId]);
                }
            }
            
            Debugger?.LogPerformance($"Expand{iteration}.PrepareDone: {(DateTime.Now - expandStartTime).TotalMilliseconds:F1}");
            
            var handle = default(JobHandle);
            // var handle = new PrepareNodeAgentPairsJob
            // {
            //     Entities = stackData.AgentEntities,
            //     Nodes = unexpandedNodes,
            //     NodeAgentPairs = nodeAgentPairs
            // }.Schedule(nodeAgentPairAmount, nodeAgentPairAmount);
            
            var requires = nodeGraph.GetRequires(unexpandedNodes, Allocator.TempJob);
            var deltas = nodeGraph.GetDeltas(unexpandedNodes, Allocator.TempJob);
            
            handle = ScheduleAllActionExpand(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter,
                nodeToParentsWriter, statesWriter, preconditionHashesWriter,
                effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                uncheckedNodes, iteration);
                
            requires.Dispose(handle);
            deltas.Dispose(handle);
            nodeAgentPairs.Dispose(handle);
            existedNodesHash.Dispose(handle);
            
            handle.Complete();

            expandedNodes.AddRange(unexpandedNodes);
            unexpandedNodes.Clear();
            
            Debugger?.LogPerformance($"Expand{iteration}.Total: {(DateTime.Now - expandStartTime).TotalMilliseconds:F1}");
        }

        protected abstract JobHandle ScheduleAllActionExpand(JobHandle handle,
            StackData stackData, NativeArray<ZephyrValueTuple<Entity, Node>> nodeAgentPairs,
            NativeArray<int> existedNodesHash,
            NativeList<ZephyrValueTuple<int, State>> requires, NativeList<ZephyrValueTuple<int, State>> deltas,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter nodeToParentsWriter,
            NativeHashMap<int, State>.ParallelWriter statesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter preconditionHashesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter effectHashesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter requireHashesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter deltaHashesWriter, 
            NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration);
        
        protected JobHandle ScheduleActionExpand<T>(JobHandle handle,
            StackData stackData, NativeArray<ZephyrValueTuple<Entity, Node>> nodeAgentPairs,
            NativeArray<int> existedNodesHash,
            NativeList<ZephyrValueTuple<int, State>> requires, NativeList<ZephyrValueTuple<int, State>> deltas,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter nodeToParentsWriter,
            NativeHashMap<int, State>.ParallelWriter statesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter preconditionHashesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter effectHashesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter requireHashesWriter, 
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter deltaHashesWriter, 
            NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration) where T : struct, IAction, IComponentData
        {
            var agentEntityAmount = stackData.AgentEntities.Length;
            var actions = new NativeHashMap<Entity, T>(agentEntityAmount, Allocator.TempJob);
            for (var agentId= 0; agentId < agentEntityAmount; agentId++)
            {
                var agentEntity = stackData.AgentEntities[agentId];
                if (!EntityManager.HasComponent<T>(agentEntity))
                {
                    continue;
                }
                actions[agentEntity] = EntityManager.GetComponentData<T>(agentEntity);
            }

            if (actions.Count() == 0)
            {
                actions.Dispose();
                return handle;
            }
            
            handle = new ActionExpandJob<T>
            {
                NodeAgentPairs = nodeAgentPairs,
                Actions = actions,
                ExistedNodesHash = existedNodesHash,
                StackData = stackData,
                Requires = requires,
                Deltas = deltas,
                NodesWriter = nodesWriter,
                NodeToParentsWriter = nodeToParentsWriter,
                StatesWriter = statesWriter,
                PreconditionHashesWriter = preconditionHashesWriter,
                EffectHashesWriter = effectHashesWriter,
                RequireHashesWriter = requireHashesWriter,
                DeltaHashesWriter = deltaHashesWriter,
                NewlyCreatedNodesWriter = newlyCreatedNodesWriter,
                ActionName = typeof(T).Name,
                Iteration = iteration
            }.Schedule(
                nodeAgentPairs.Length, nodeAgentPairs.Length, handle);

            actions.Dispose(handle);
            return handle;
        }
    }
}