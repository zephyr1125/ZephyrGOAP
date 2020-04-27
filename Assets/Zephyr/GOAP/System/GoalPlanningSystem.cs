using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Debugger;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System.GoalManage;
using Zephyr.GOAP.System.GoapPlanningJob;

namespace Zephyr.GOAP.System
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SensorSystemGroup))]
    [UpdateAfter(typeof(AgentGoalMonitorSystemGroup))]
    public class GoalPlanningSystem : ComponentSystem
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
        
        private EntityQuery _agentQuery, _goalQuery;

        public IGoapDebugger Debugger;

        private EntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            _agentQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new []
                {
                    ComponentType.ReadOnly<Agent>(),
                    ComponentType.ReadOnly<Translation>(), 
                    ComponentType.ReadOnly<MaxMoveSpeed>(), 
                },
                None = new []
                {
                    ComponentType.ReadOnly<Node>(),
                    ComponentType.ReadOnly<CurrentGoal>(), 
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

            var nodeGraph = new NodeGraph(512, Allocator.TempJob);

            var goalPrecondition = new StateGroup();
            var goalEffects = new StateGroup();
            var goalNode = new Node(ref goalPrecondition, ref goalEffects, ref goalStates,
                new NativeString64("goal"), 0, 0, 0, Entity.Null);
            
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
                    ref unexpandedNodes)) foundPlan = true;

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
                Debugger?.Log($"goal plan failed : {reason}");
                
                Utils.NextGoalState<IdleGoal, PlanFailedGoal>(goal.GoalEntity,
                    EntityManager, Time.ElapsedTime);

                var buffer = EntityManager.AddBuffer<FailedPlanLog>(goal.GoalEntity);
                buffer.Add(new FailedPlanLog {Time = (float)Time.ElapsedTime});
                
                Debugger?.SetNodeGraph(ref nodeGraph, EntityManager);
            }
            else
            {
                UnifyNodeStates(ref stackData.CurrentStates, ref nodeGraph);
                ApplyNodeNavigatingSubjects(ref nodeGraph);
                //寻路
                var nodeAgentInfos = new NativeMultiHashMap<int, NodeAgentInfo>(nodeGraph.Length(), Allocator.TempJob);
                var nodeTotalTimes = new NativeHashMap<int, float>(nodeGraph.Length(), Allocator.TempJob);
                var pathResult = FindPath(ref nodeGraph, ref stackData,
                    ref agentMoveSpeeds, ref agentStartTimes, ref nodeAgentInfos, ref nodeTotalTimes);
                SavePath(ref pathResult, ref nodeGraph, default).Complete();
                
                Debugger?.SetNodeGraph(ref nodeGraph, EntityManager);
                Debugger?.SetPathResult(ref pathResult);
                Debugger?.SetNodeAgentInfos(EntityManager, ref nodeAgentInfos);
                Debugger?.SetNodeTotalTimes(ref nodeTotalTimes);
                
                pathResult.Dispose();
                nodeAgentInfos.Dispose();
                nodeTotalTimes.Dispose();

                Utils.NextGoalState<IdleGoal, ExecutingGoal>(goal.GoalEntity,
                    EntityManager, Time.ElapsedTime);
            }

            uncheckedNodes.Dispose();
            unexpandedNodes.Dispose();
            expandedNodes.Dispose();
            nodeGraph.Dispose();

            Debugger?.LogDone();
            
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
            ref NativeHashMap<int, float> nodeTotalTimes)
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
                NodeTotalTimes = nodeTotalTimes
            };
            var handle = pathFindingJob.Schedule();
            handle.Complete();

            return pathResult;
        }

        /// <summary>
        /// 把NodeGraph里所有宽泛的state和precondition，用其child的具体effect替代
        /// </summary>
        /// <param name="currentStates"></param>
        /// <param name="nodeGraph"></param>
        private void UnifyNodeStates(ref StateGroup currentStates, ref NodeGraph nodeGraph)
        {
            //start -> goal, 不包含start
            var startNode = nodeGraph.GetStartNode();
            var node = startNode;
            var edges = new NativeQueue<Edge>(Allocator.Temp);
            nodeGraph.GetEdges(node, ref edges);
            
            while (edges.Count > 0)
            {
                var edge = edges.Dequeue();
                node = nodeGraph[edge.ParentHash];
                var child = nodeGraph[edge.ChildHash];
               
                var nodeStates = nodeGraph.GetNodeStates(node, Allocator.Temp);
                var nodePreconditions = nodeGraph.GetNodePreconditions(node, Allocator.Temp);
                StateGroup childStates;
                var isLastNode = child.Equals(startNode);
                if (isLastNode)
                {
                    //对于start之前最后一个node，需要与世界状态作比对
                    childStates = currentStates;
                }
                else
                {
                    childStates = nodeGraph.GetNodeStates(child, Allocator.Temp);
                    var childEffects = nodeGraph.GetNodeEffects(child, Allocator.Temp);
                    var childPreconditions = nodeGraph.GetNodePreconditions(child, Allocator.Temp);
                    
                    childStates.Sub(ref childPreconditions, out var removedStates, Allocator.Temp);
                    removedStates.Dispose();
                    childStates.Merge(childEffects);
                    
                    childEffects.Dispose();
                    childPreconditions.Dispose();
                }
                
                foreach (var state in nodeStates)
                {
                    if (!state.IsScopeState()) continue;
                    //在子节点中寻找对应的具体effect
                    var childSpecificEffect = default(State);
                    foreach (var childEffect in childStates)
                    {
                        if (!childEffect.BelongTo(state)) continue;
                        childSpecificEffect = childEffect;
                        break;
                    }
                    if (childSpecificEffect.Equals(default)) continue;
                    //以子节点的具体effect替换自己的宽泛state
                    nodeGraph.ReplaceNodeState(node, state, childSpecificEffect);
                    //precondition里一样的state也如此替换
                    foreach (var nodePrecondition in nodePreconditions)
                    {
                        if (!nodePrecondition.Equals(state)) continue;
                        nodeGraph.ReplaceNodePrecondition(node, nodePrecondition, childSpecificEffect);
                    }
                }
                
                nodeStates.Dispose();
                nodePreconditions.Dispose();
                if(!isLastNode)childStates.Dispose();
                
                nodeGraph.GetEdges(node, ref edges);
            } 

            edges.Dispose();
        }

        /// <summary>
        /// 在明确了所有节点的具体state之后，赋予各自导航目标
        /// </summary>
        /// <param name="nodeGraph"></param>
        private void ApplyNodeNavigatingSubjects(ref NodeGraph nodeGraph)
        {
            //start -> goal, 不包含start
            var startNode = nodeGraph.GetStartNode();
            var node = startNode;
            var edges = new NativeQueue<Edge>(Allocator.Temp);
            nodeGraph.GetEdges(node, ref edges);

            while (edges.Count > 0)
            {
                var edge = edges.Dequeue();
                var nodeHash = edge.ParentHash;
                node = nodeGraph[nodeHash];
                
                switch (node.NavigatingSubjectType)
                {
                    case NodeNavigatingSubjectType.Null:
                        continue;
                    case NodeNavigatingSubjectType.PreconditionTarget:
                        var preconditions = nodeGraph.GetNodePreconditions(node, Allocator.Temp);
                        var subjectPrecondition = preconditions[node.NavigatingSubjectId];
                        node.NavigatingSubject = subjectPrecondition.Target;
                        node.NavigatingSubjectPosition = subjectPrecondition.Position;
                        preconditions.Dispose();
                        break;
                    case NodeNavigatingSubjectType.EffectTarget:
                        var effects = nodeGraph.GetNodeEffects(node, Allocator.Temp);
                        var subjectEffect = effects[node.NavigatingSubjectId];
                        node.NavigatingSubject = subjectEffect.Target;
                        node.NavigatingSubjectPosition = subjectEffect.Position;
                        effects.Dispose();
                        break;
                }

                nodeGraph[nodeHash] = node;
                
                nodeGraph.GetEdges(node, ref edges);
            }
            
            edges.Dispose();
        }

        private JobHandle SavePath([ReadOnly]ref NativeList<Node> pathResult, [ReadOnly]ref NodeGraph nodeGraph, JobHandle inputDeps)
        {
            //首先保存path上的所有node和state到新entity
            var pathNodes = pathResult.AsArray();
            var pathEntities = new NativeArray<Entity>(pathResult.Length, Allocator.TempJob);
            var pathSavingJob = new PathSavingJob
            {
                PathResult = pathResult.AsArray(),
                ECBuffer = _ecbSystem.CreateCommandBuffer().ToConcurrent(),
                NodeGraph = nodeGraph,
                PathEntities = pathEntities
            };
            var pathSavingJobHandle = pathSavingJob.Schedule(pathResult.Length, inputDeps);
            _ecbSystem.AddJobHandleForProducer(pathSavingJobHandle);
            
            //然后建立依赖
            var bufferStates = GetBufferFromEntity<State>();
            var pathAddDependencyJob = new PathAddDependencyJob
            {
                PathEntities = pathEntities,
                PathNodes = pathNodes,
                BufferStates = bufferStates
            };
            var pathAddDependencyJobHandle =
                pathAddDependencyJob.Schedule(this, pathSavingJobHandle);
            
            return pathAddDependencyJobHandle;
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
        public bool CheckNodes(ref NativeHashMap<int, Node> uncheckedNodes, ref NodeGraph nodeGraph,
            ref StateGroup currentStates, ref NativeList<Node> unexpandedNodes)
        {
            bool foundPlan = false;
            var nodes = uncheckedNodes.GetValueArray(Allocator.Temp);
            foreach (var uncheckedNode in nodes)
            {
                Debugger?.Log("check node: "+uncheckedNode.Name);
                nodeGraph.CleanAllDuplicateStates(uncheckedNode);
                
                var uncheckedStates = nodeGraph.GetNodeStates(uncheckedNode, Allocator.Temp);
                uncheckedStates.Sub(ref currentStates, out var removedStates, Allocator.Temp);
                
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
                
                var children = nodeGraph.GetChildren(uncheckedNode, Allocator.Temp);
                if (children.Length > 0)
                {
                    var edges = nodeGraph.GetEdgeToParents(uncheckedNode);
                    while (edges.MoveNext())
                    {
                        if (!children.Contains(edges.Current.ParentHash)) continue;
                        loop = true;
                        nodeGraph.RemoveEdge(uncheckedNode, nodeGraph[edges.Current.ParentHash]);
                        break;
                    }
                }
                children.Dispose();

                if (!loop)
                {
                    //没有产生循环，则把此node置入待展开列表
                    //需要把他的state里符合currentState的移除掉，以避免再次被展开
                    if (!foundPlan)
                    {
                        for (var i = 0; i < removedStates.Length(); i++)
                        {
                            nodeGraph.RemoveNodeState(uncheckedNode, removedStates[i]);
                        }
                    }
                    unexpandedNodes.Add(uncheckedNode);
                }

                removedStates.Dispose();
                uncheckedStates.Dispose();
            }

            nodes.Dispose();
            uncheckedNodes.Clear();
            return foundPlan;
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
            nodeGraph.GetNodeStates(ref unexpandedNodes, out var nodeStateIndices,
                out var nodeStates, Allocator.TempJob);
            var nodesWriter = nodeGraph.NodesWriter;
            var nodeToParentWriter = nodeGraph.NodeToParentWriter;
            var nodeStateIndicesWriter = nodeGraph.NodeStateIndicesWriter;
            var nodeStatesWriter = nodeGraph.NodeStatesWriter;
            var preconditionIndicesWriter = nodeGraph.PreconditionIndicesWriter;
            var preconditionsWriter = nodeGraph.PreconditionsWriter;
            var effectIndicesWriter = nodeGraph.EffectIndicesWriter;
            var effectsWriter = nodeGraph.EffectsWriter;
            
            var handle = default(JobHandle);
            handle = ScheduleActionExpand<DropItemAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStateIndices, ref nodeStates, nodesWriter,
                nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                preconditionIndicesWriter, preconditionsWriter, effectIndicesWriter, effectsWriter,
                ref uncheckedNodes, iteration);
            handle = ScheduleActionExpand<PickItemAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStateIndices, ref nodeStates, nodesWriter,
                nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                preconditionIndicesWriter, preconditionsWriter, effectIndicesWriter, effectsWriter,
                ref uncheckedNodes, iteration);
            handle = ScheduleActionExpand<EatAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStateIndices, ref nodeStates, nodesWriter,
                nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                preconditionIndicesWriter, preconditionsWriter, effectIndicesWriter, effectsWriter,
                ref uncheckedNodes, iteration);
            handle = ScheduleActionExpand<CookAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStateIndices, ref nodeStates, nodesWriter,
                nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                preconditionIndicesWriter, preconditionsWriter, effectIndicesWriter, effectsWriter,
                ref uncheckedNodes, iteration);
            handle = ScheduleActionExpand<WanderAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStateIndices, ref nodeStates, nodesWriter,
                nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                preconditionIndicesWriter, preconditionsWriter, effectIndicesWriter, effectsWriter,
                ref uncheckedNodes, iteration);
            handle = ScheduleActionExpand<CollectAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStateIndices, ref nodeStates, nodesWriter,
                nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                preconditionIndicesWriter, preconditionsWriter, effectIndicesWriter, effectsWriter,
                ref uncheckedNodes, iteration);
            handle = ScheduleActionExpand<PickRawAction>(handle,ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStateIndices, ref nodeStates, nodesWriter,
                nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                preconditionIndicesWriter, preconditionsWriter, effectIndicesWriter, effectsWriter,
                ref uncheckedNodes, iteration);
            handle = ScheduleActionExpand<DropRawAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStateIndices, ref nodeStates, nodesWriter,
                nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                preconditionIndicesWriter, preconditionsWriter, effectIndicesWriter, effectsWriter,
                ref uncheckedNodes, iteration);

            handle.Complete();
            existedNodesHash.Dispose();
            nodeStateIndices.Dispose();
            nodeStates.Dispose();
            
            expandedNodes.AddRange(unexpandedNodes);
            unexpandedNodes.Clear();
        }
        
        private JobHandle ScheduleActionExpand<T>(JobHandle handle,
            ref StackData stackData, ref NativeList<Node> unexpandedNodes,
            ref NativeArray<int> existedNodesHash,
            ref NativeList<int> nodeStateIndices, ref NativeList<State>  nodeStates,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeMultiHashMap<int, Edge>.ParallelWriter nodeToParentWriter, 
            NativeList<int>.ParallelWriter nodeStateIndicesWriter,
            NativeList<State>.ParallelWriter nodeStatesWriter, 
            NativeList<int>.ParallelWriter preconditionIndicesWriter,
            NativeList<State>.ParallelWriter preconditionsWriter, 
            NativeList<int>.ParallelWriter effectIndicesWriter,
            NativeList<State>.ParallelWriter effectsWriter, 
            ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration) where T : struct, IAction, IComponentData
        {
            for (var i = 0; i < stackData.AgentEntities.Length; i++)
            {
                stackData.CurrentAgentId = i;
                var agentEntity = stackData.AgentEntities[i];
                if (EntityManager.HasComponent<T>(agentEntity))
                {
                    var action = EntityManager.GetComponentData<T>(agentEntity);
                    handle = new ActionExpandJob<T>(ref unexpandedNodes, ref existedNodesHash,
                        ref stackData, ref nodeStateIndices, ref nodeStates, nodesWriter,
                        nodeToParentWriter, nodeStateIndicesWriter, nodeStatesWriter,
                        preconditionIndicesWriter, preconditionsWriter,
                        effectIndicesWriter, effectsWriter,
                        ref newlyCreatedNodesWriter, iteration, action).Schedule(
                        unexpandedNodes, 6, handle);
                }
            }
            return handle;
        }
    }
}