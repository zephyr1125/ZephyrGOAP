using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Debugger;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System.GoalManage;
using Zephyr.GOAP.System.GoapPlanningJob;

namespace Zephyr.GOAP.System
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
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
        /// 对于一种action进行展开的agent的上限，以避免node graph过度膨胀
        /// </summary>
        public int MaxAgentForAction = 3;
        
        private EntityQuery _agentQuery, _goalQuery;

        public IGoapDebugger Debugger;

        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            _agentQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new []
                {
                    ComponentType.ReadOnly<Agent>(),
                    ComponentType.ReadOnly<Translation>(), 
                    ComponentType.ReadOnly<MaxMoveSpeed>(), 
                }
            });
            _goalQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] {ComponentType.ReadOnly<Goal>(), ComponentType.ReadOnly<IdleGoal>(), },
            });
        }

        protected override void OnUpdate()
        {
            //如果没有空闲agent或者没有任务,不运行
            var agentEntities = _agentQuery.ToEntityArray(Allocator.TempJob);
            if (agentEntities.Length <= 0 || _goalQuery.CalculateEntityCount() <= 0)
            {
                agentEntities.Dispose();
                return;
            }
            
            //找到最急需的一个goal
            var goal = GetMostPriorityGoal();
            var goalStates = new StateGroup(1, Allocator.TempJob) {goal.State};

            //从currentState的存储Entity上拿取current states
            var currentStateBuffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            
            var agentTranslations = _agentQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            var agentMoveSpeeds = _agentQuery.ToComponentDataArray<MaxMoveSpeed>(Allocator.TempJob);
            var agentStartTimes = new NativeArray<float>(agentTranslations.Length, Allocator.TempJob);
            
            //组织StackData
            var stackData = new StackData(ref agentEntities, ref agentTranslations,
                new StateGroup(ref currentStateBuffer, Allocator.TempJob));
            agentTranslations.Dispose();

            Debugger?.StartLog(EntityManager);
            Debugger?.SetCurrentStates(ref stackData.CurrentStates, EntityManager);

            var uncheckedNodes = new NativeHashMap<int, Node>(32, Allocator.TempJob);
            var uncheckedNodesWriter = uncheckedNodes.AsParallelWriter();
            var unexpandedNodes = new NativeList<Node>(Allocator.TempJob);
            var expandedNodes = new NativeList<Node>(Allocator.TempJob);

            var nodeGraph = new NodeGraph(512, ref currentStateBuffer, Allocator.TempJob);

            var goalPrecondition = new StateGroup();
            var goalEffects = new StateGroup();
            var goalNode = new Node(ref goalPrecondition, ref goalEffects, ref goalStates,
                "goal", 0, 0, 0, Entity.Null);
            
            //goalNode进入graph
            nodeGraph.SetGoalNode(goalNode, ref goalStates);

            //goalNode进入待检查列表
            uncheckedNodes.Add(goalNode.HashCode, goalNode);

            var iteration = 1; //goal node iteration is 0
            var foundPlan = false;
            
            while (uncheckedNodes.Count() > 0 && iteration < ExpandIterations)
            {
                Debugger?.Log("Loop:");
                //对待检查列表进行检查（与CurrentStates比对）
                if (CheckNodes(ref uncheckedNodes, ref nodeGraph, ref stackData.CurrentStates,
                    ref unexpandedNodes, iteration)) foundPlan = true;

                //对待展开列表进行展开，并挑选进入待检查和展开后列表
                ExpandNodes(ref unexpandedNodes, ref stackData, ref nodeGraph,
                    ref uncheckedNodesWriter, ref expandedNodes, iteration);

                //直至待展开列表为空或Early Exit
                iteration++;
            }

            var nodes = nodeGraph.GetNodes(Allocator.Temp);
            Debugger?.Log($"{nodes.Length} nodes in graph");
            nodes.Dispose();

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
                
                Debugger?.SetNodeGraph(ref nodeGraph, EntityManager);
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
                
                var pathNodes = FindPath(ref nodeGraph, ref stackData,
                    ref agentMoveSpeeds, ref agentStartTimes, ref nodeAgentInfos, ref nodeTotalTimes, ref pathNodesEstimateNavigateTime,
                    ref pathNodeNavigateSubjects, ref pathNodeSpecifiedPreconditionIndices, ref pathNodeSpecifiedPreconditions,
                    ref rewardSum);
                
                SavePath(ref pathNodes, ref nodeGraph, ref pathNodesEstimateNavigateTime, 
                    ref pathNodeNavigateSubjects, ref pathNodeSpecifiedPreconditionIndices, ref pathNodeSpecifiedPreconditions,
                    goal.GoalEntity, out var pathEntities);
                
                //保存总预测时间
                var totalTime = nodeTotalTimes[pathNodes[0].HashCode];
                goal.EstimateTimeLength = totalTime;
                EntityManager.SetComponentData(goal.GoalEntity, goal);

                Debugger?.SetNodeGraph(ref nodeGraph, EntityManager);
                Debugger?.SetNodeAgentInfos(EntityManager, ref nodeAgentInfos);
                Debugger?.SetNodeTotalTimes(ref nodeTotalTimes);
                Debugger?.SetPathResult(EntityManager, ref pathEntities, ref pathNodes);
                Debugger?.SetSpecifiedPreconditions(EntityManager,
                    ref pathNodeSpecifiedPreconditionIndices, ref pathNodeSpecifiedPreconditions);
                Debugger?.SetRewardSum(ref rewardSum);

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
            
            goalStates.Dispose();
            agentMoveSpeeds.Dispose();
            agentStartTimes.Dispose();
            agentEntities.Dispose();
            stackData.Dispose();
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
        
        private NativeList<Node> FindPath(ref NodeGraph nodeGraph, ref StackData stackData,
            ref NativeArray<MaxMoveSpeed> agentMoveSpeed, ref NativeArray<float> agentStartTime,
            ref NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos,
            ref NativeHashMap<int, float> nodeTotalTimes, ref NativeHashMap<int, float> nodeNavigateStartTimes,
            ref NativeHashMap<int, Entity> nodeNavigateSubjects,
            ref NativeList<int> pathNodeSpecifiedPreconditionIndices,
            ref NativeList<State> pathNodeSpecifiedPreconditions,
            ref NativeHashMap<int, float> rewardSum)
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

        private void SavePath([ReadOnly]ref NativeList<Node> pathNodes, [ReadOnly]ref NodeGraph nodeGraph,
            [ReadOnly]ref NativeHashMap<int, float> pathNodesEstimateNavigateTime,
            [ReadOnly]ref NativeHashMap<int, Entity> pathNodeNavigateSubjects,
            [ReadOnly]ref NativeList<int> pathNodeSpecifiedPreconditionIndices,
            [ReadOnly]ref NativeList<State> pathNodeSpecifiedPreconditions, Entity goalEntity,
            out NativeArray<Entity> pathEntities)
        {
            pathEntities = new NativeArray<Entity>(pathNodes.Length, Allocator.Temp);
            var pathPreconditionHashes = new NativeList<ValueTuple<int, int>>(Allocator.Temp);
            for (var i = 0; i < pathNodes.Length; i++)
            {
                var node = pathNodes[i];
                var effects = nodeGraph.GetNodeEffects(node, Allocator.Temp);

                var entity = EntityManager.CreateEntity();
                pathEntities[i] = entity;
                // add states & dependencies
                var stateBuffer = EntityManager.AddBuffer<State>(entity);
                //precondition不从NodeGraph来，而是用寻路时得到的明确版本
                for (var j = 0; j < pathNodeSpecifiedPreconditionIndices.Length; j++)
                {
                    var specifiedPrecondition = pathNodeSpecifiedPreconditions[j];
                    if (!pathNodeSpecifiedPreconditionIndices[j].Equals(node.HashCode)) continue;
                    stateBuffer.Add(specifiedPrecondition);
                    node.PreconditionsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
                    pathPreconditionHashes.Add((node.HashCode, specifiedPrecondition.GetHashCode()));
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
                        if (!pathPreconditionHashes.Contains((nodeHash, otherEffect.GetHashCode())))
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
        /// 与CurrentStates一致的state被从Node中移除
        /// 出现全部State都被移除的Node时，视为找到Plan，其后追加空Node作为起点，可以考虑此时EarlyExit
        /// 对于还有State不满足的Node进入待展开列表
        /// </summary>
        /// <param name="uncheckedNodes"></param>
        /// <param name="nodeGraph"></param>
        /// <param name="currentStates"></param>
        /// <param name="unexpandedNodes"></param>
        /// <param name="iteration"></param>
        public bool CheckNodes(ref NativeHashMap<int, Node> uncheckedNodes, ref NodeGraph nodeGraph,
            ref StateGroup currentStates, ref NativeList<Node> unexpandedNodes, int iteration)
        {
            bool foundPlan = false;
            var nodes = uncheckedNodes.GetValueArray(Allocator.Temp);
            foreach (var uncheckedNode in nodes)
            {
                Debugger?.Log("check node: "+uncheckedNode.Name);
                nodeGraph.CleanAllDuplicateStates(uncheckedNode);
                
                var uncheckedStates = nodeGraph.GetNodeStates(uncheckedNode, Allocator.Temp, true);
                uncheckedStates.AND(currentStates);
                //对这些state调整后重新放回nodeGraph
                nodeGraph.AddNodeStates(uncheckedStates, uncheckedNode.HashCode);
                
                //为了避免没有state的node(例如wander)与startNode有相同的hash，这种node被强制给了一个空state
                //因此在只有1个state且内容为空时，也应视为找到了plan
                if (uncheckedStates.Length() <= 0 ||
                    (uncheckedStates.Length()==1 && uncheckedStates[0].Equals(default)))
                {
                    //找到Plan，追加起点Node
                    Debugger?.Log("found plan: "+uncheckedNode.Name);
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
                    if (uncheckedStates.Length() > 0)
                    {
                        unexpandedNodes.Add(uncheckedNode);
                    }
                }
                else
                {
                    //否则的话此node进入dead end 列表，以供debug查看
                    nodeGraph.AddDeadEndNode(uncheckedNode.HashCode);
                }
                uncheckedStates.Dispose();
            }

            nodes.Dispose();
            uncheckedNodes.Clear();
            return foundPlan;
        }

        private static bool RemoveLoop(NodeGraph nodeGraph, int nodeHash, int parentHash)
        {
            var children = nodeGraph.GetChildren(nodeHash, Allocator.Temp);
            for (var childId = 0; childId < children.Length; childId++)
            {
                if (!parentHash.Equals(children[childId]))
                {
                    RemoveLoop(nodeGraph, children[childId], parentHash);
                    continue;   
                }
                nodeGraph.RemoveConnection(nodeHash, parentHash);
                return true;
            }

            return false;
        }

        public void ExpandNodes(ref NativeList<Node> unexpandedNodes, ref StackData stackData,
            ref NodeGraph nodeGraph, ref NativeHashMap<int, Node>.ParallelWriter uncheckedNodes, ref NativeList<Node> expandedNodes,
            int iteration)
        {
            if (unexpandedNodes.Length <= 0) return;
            
            foreach (var node in unexpandedNodes)
            {
                Debugger?.Log("expanding node: "+node.Name+", "+node.GetHashCode());
            }

            var existedNodesHash = nodeGraph.GetAllNodesHash(Allocator.TempJob);
            nodeGraph.GetNodeStates(ref unexpandedNodes,
                out var nodeStates, Allocator.TempJob);
            var nodesWriter = nodeGraph.NodesWriter;
            var nodeToParentsWriter = nodeGraph.NodeToParentsWriter;
            var nodeStatesWriter = nodeGraph.NodeStatesWriter;
            var preconditionsWriter = nodeGraph.PreconditionsWriter;
            var effectsWriter = nodeGraph.EffectsWriter;
            
            var handle = default(JobHandle);
            
            handle = ScheduleAllActionExpand(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, preconditionsWriter, effectsWriter,
                ref uncheckedNodes, iteration);

            handle.Complete();
            existedNodesHash.Dispose();
            nodeStates.Dispose();
            
            expandedNodes.AddRange(unexpandedNodes);
            unexpandedNodes.Clear();
        }

        protected abstract JobHandle ScheduleAllActionExpand(JobHandle handle,
            ref StackData stackData, ref NativeList<Node> unexpandedNodes,
            ref NativeArray<int> existedNodesHash,
            ref NativeList<ValueTuple<int, State>> nodeStates,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeList<ValueTuple<int, int>>.ParallelWriter nodeToParentsWriter,
            NativeList<ValueTuple<int, State>>.ParallelWriter nodeStatesWriter,
            NativeList<ValueTuple<int, State>>.ParallelWriter preconditionsWriter,
            NativeList<ValueTuple<int, State>>.ParallelWriter effectsWriter,
            ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration);
        
        protected JobHandle ScheduleActionExpand<T>(JobHandle handle,
            ref StackData stackData, ref NativeList<Node> unexpandedNodes,
            ref NativeArray<int> existedNodesHash,
            ref NativeList<ValueTuple<int, State>> nodeStates,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeList<ValueTuple<int, int>>.ParallelWriter nodeToParentsWriter,
            NativeList<ValueTuple<int, State>>.ParallelWriter nodeStatesWriter, 
            NativeList<ValueTuple<int, State>>.ParallelWriter preconditionsWriter, 
            NativeList<ValueTuple<int, State>>.ParallelWriter effectsWriter, 
            ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration) where T : struct, IAction, IComponentData
        {
            var agentCount = 0;
            for (var i = 0; i < stackData.AgentEntities.Length; i++)
            {
                stackData.CurrentAgentId = i;
                var agentEntity = stackData.AgentEntities[i];
                if (!EntityManager.HasComponent<T>(agentEntity)) continue;

                var sameAgent = false;
                for (var nodeId = 0; nodeId < unexpandedNodes.Length; nodeId++)
                {
                    if (sameAgent) break;
                    var unexpandedNode = unexpandedNodes[nodeId];
                    for (var stateId = 0; stateId < nodeStates.Length; stateId++)
                    {
                        var (hash, state) = nodeStates[stateId];
                        if (!hash.Equals(unexpandedNode.HashCode)) continue;
                        if (!state.Target.Equals(agentEntity)) continue;
                        sameAgent = true;
                        break;
                    }
                }
                
                //即使超过了agent计数，但是这个agent是goal的target的话也要尝试展开
                if (agentCount >= MaxAgentForAction && !sameAgent)
                {
                    continue;
                }
                
                var action = EntityManager.GetComponentData<T>(agentEntity);
                handle = new ActionExpandJob<T>(ref unexpandedNodes, ref existedNodesHash,
                    ref stackData, ref nodeStates, nodesWriter,
                    nodeToParentsWriter, nodeStatesWriter, preconditionsWriter, effectsWriter,
                    ref newlyCreatedNodesWriter, iteration, action).Schedule(
                    unexpandedNodes, 6, handle);
                
                //优化点：如果能够执行的agent较多，只展开其中前几个
                agentCount++;
            }
            return handle;
        }
    }
}