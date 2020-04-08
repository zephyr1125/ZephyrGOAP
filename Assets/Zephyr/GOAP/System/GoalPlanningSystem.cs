using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
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

        protected override void OnCreate()
        {
            base.OnCreate();
            _agentQuery = GetEntityQuery(new EntityQueryDesc()
            {
                All = new []
                {
                    ComponentType.ReadOnly<Agent>(),
                    ComponentType.ReadOnly<Translation>(), 
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
            
            //组织StackData
            var agentTranslations = _agentQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            var stackData = new StackData(ref agentEntities, ref agentTranslations,
                new StateGroup(ref currentStateBuffer, Allocator.TempJob));
            agentTranslations.Dispose();

            Debugger?.StartLog(EntityManager);
            Debugger?.SetCurrentStates(ref stackData.CurrentStates, EntityManager);

            var uncheckedNodes = new NativeList<Node>(Allocator.TempJob);
            var unexpandedNodes = new NativeList<Node>(Allocator.TempJob);
            var expandedNodes = new NativeList<Node>(Allocator.TempJob);

            var nodeGraph = new NodeGraph(512, Allocator.TempJob);

            var goalPrecondition = new StateGroup();
            var goalEffects = new StateGroup();
            var goalNode = new Node(ref goalPrecondition, ref goalEffects, ref goalStates,
                new NativeString64("goal"), 0, 0, Entity.Null);
            
            //goalNode进入graph
            nodeGraph.SetGoalNode(goalNode, ref goalStates);

            //goalNode进入待检查列表
            uncheckedNodes.Add(goalNode);

            var iteration = 1; //goal node iteration is 0
            var foundPlan = false;
            
            while (uncheckedNodes.Length > 0 && iteration < ExpandIterations)
            {
                Debugger?.Log("Loop:");
                //对待检查列表进行检查（与CurrentStates比对）
                if (CheckNodes(ref uncheckedNodes, ref nodeGraph, ref stackData.CurrentStates,
                    ref unexpandedNodes)) foundPlan = true;

                //对待展开列表进行展开，并挑选进入待检查和展开后列表
                ExpandNodes(ref unexpandedNodes, ref stackData, ref nodeGraph,
                    ref uncheckedNodes, ref expandedNodes, iteration);

                //直至待展开列表为空或Early Exit
                iteration++;
            }

            var nodes = nodeGraph.GetNodes(Allocator.Temp);
            Debug.Log($"{nodes.Length} nodes in graph");
            nodes.Dispose();

            if (!foundPlan)
            {
                //在展开阶段没有能够链接到current state的话，就没有找到规划，也就不用继续寻路了
                //目前对于规划失败的情况，就直接转入NoGoal状态
                Debugger?.Log("goal plan failed : " + goalStates);
                
                Utils.NextGoalState<IdleGoal, PlanFailedGoal>(goal.GoalEntity,
                    EntityManager, Time.ElapsedTime);

                var buffer = EntityManager.AddBuffer<FailedPlanLog>(goal.GoalEntity);
                buffer.Add(new FailedPlanLog {Time = (float)Time.ElapsedTime});
                
                Debugger?.SetNodeGraph(ref nodeGraph, EntityManager);
            }
            else
            {
                //寻路
                var pathResult = FindPath(ref nodeGraph);
                UnifyPathNodeStates(ref stackData.CurrentStates, ref nodeGraph, ref pathResult);
                ApplyPathNodeNavigatingSubjects(ref nodeGraph, ref pathResult);
                SavePath(ref pathResult, ref nodeGraph);
                
                Debugger?.SetNodeGraph(ref nodeGraph, EntityManager);
                Debugger?.SetPathResult(ref pathResult);
                
                pathResult.Dispose();

                Utils.NextGoalState<IdleGoal, ExecutingGoal>(goal.GoalEntity,
                    EntityManager, Time.ElapsedTime);
            }

            uncheckedNodes.Dispose();
            unexpandedNodes.Dispose();
            expandedNodes.Dispose();
            nodeGraph.Dispose();

            Debugger?.LogDone();
            
            goalStates.Dispose();
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
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="createTime"></param>
        /// <returns></returns>
        private float CalcGoalPriority(Priority priority, double createTime)
        {
            return (float) (Priority.Max - priority + createTime / 60);
        }
        
        private NativeList<Node> FindPath(ref NodeGraph nodeGraph)
        {
            var pathResult = new NativeList<Node>(Allocator.TempJob);
            var pathFindingJob = new PathFindingJob
            {
                StartNodeId = nodeGraph.GetStartNode().GetHashCode(),
                GoalNodeId = nodeGraph.GetGoalNode().GetHashCode(),
                IterationLimit = PathFindingIterations,
                NodeGraph = nodeGraph,
                PathNodeLimit = PathNodeLimit,
                Result = pathResult
            };
            var handle = pathFindingJob.Schedule();
            handle.Complete();

            return pathResult;
        }

        /// <summary>
        /// 把path中所有宽泛的state和precondition，用其child的具体effect替代
        /// </summary>
        /// <param name="currentStates"></param>
        /// <param name="nodeGraph"></param>
        /// <param name="pathResult"></param>
        private void UnifyPathNodeStates(ref StateGroup currentStates, ref NodeGraph nodeGraph,
            ref NativeList<Node> pathResult)
        {
            //goal -> start, 不包含start
            for (var i = pathResult.Length - 1; i >= 0; i--)
            {
                var node = pathResult[i];
                
                var nodeStates = nodeGraph.GetNodeStates(node, Allocator.Temp);
                var nodePreconditions = nodeGraph.GetNodePreconditions(node, Allocator.Temp);
                var childStates = new StateGroup();
                if (i == pathResult.Length - 1)
                {
                    //对于最后一个node，需要与世界状态作比对
                    childStates = currentStates;
                }
                else
                {
                    var child = pathResult[i + 1];
                    childStates = nodeGraph.GetNodeStates(child, Allocator.Temp);
                    var childEffects = nodeGraph.GetNodeEffects(child, Allocator.Temp);
                    var childPreconditions = nodeGraph.GetNodePreconditions(child, Allocator.Temp);
                    
                    childStates.Sub(ref childPreconditions);
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
            }
        }

        /// <summary>
        /// 在明确了所有节点的具体state之后，赋予各自导航目标
        /// </summary>
        /// <param name="nodeGraph"></param>
        /// <param name="pathResult"></param>
        private void ApplyPathNodeNavigatingSubjects(ref NodeGraph nodeGraph, ref NativeList<Node> pathResult)
        {
            for (var i = 0; i < pathResult.Length; i++)
            {
                var node = pathResult[i];
                switch (node.NavigatingSubjectType)
                {
                    case NodeNavigatingSubjectType.Null:
                        continue;
                    case NodeNavigatingSubjectType.PreconditionTarget:
                        var preconditions = nodeGraph.GetNodePreconditions(node, Allocator.Temp);
                        node.NavigatingSubject = preconditions[node.NavigatingSubjectId].Target;
                        preconditions.Dispose();
                        break;
                    case NodeNavigatingSubjectType.EffectTarget:
                        var effects = nodeGraph.GetNodeEffects(node, Allocator.Temp);
                        node.NavigatingSubject = effects[node.NavigatingSubjectId].Target;
                        effects.Dispose();
                        break;
                }

                pathResult[i] = node;
            }
        }

        private void SavePath(ref NativeList<Node> pathResult, ref NodeGraph nodeGraph)
        {
            //保存结果
            // var nodeBuffer = EntityManager.AddBuffer<Node>(agentEntity);
            // var stateBuffer =
            //     EntityManager.GetBuffer<State>(agentEntity); //已经在创建goal的时候创建了state buffer以容纳goal state
            // for (var i = pathResult.Length-1; i > 0; i--)    //path的0号为goal，不存
            // {
            //     var node = pathResult[i];
            //     var preconditions = nodeGraph.GetNodePreconditions(node, Allocator.Temp);
            //     var effects = nodeGraph.GetNodeEffects(node, Allocator.Temp);
            //
            //     foreach (var precondition in preconditions)
            //     {
            //         stateBuffer.Add(precondition);
            //         node.PreconditionsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
            //     }
            //
            //     foreach (var effect in effects)
            //     {
            //         stateBuffer.Add(effect);
            //         node.EffectsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
            //     }
            //
            //     nodeBuffer.Add(node);
            //
            //     preconditions.Dispose();
            //     effects.Dispose();
            // }
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
        public bool CheckNodes(ref NativeList<Node> uncheckedNodes, ref NodeGraph nodeGraph,
            ref StateGroup currentStates, ref NativeList<Node> unexpandedNodes)
        {
            bool foundPlan = false;
            foreach (var uncheckedNode in uncheckedNodes)
            {
                Debugger?.Log("check node: "+uncheckedNode.Name);
                nodeGraph.CleanAllDuplicateStates(uncheckedNode);
                
                var uncheckedStates = nodeGraph.GetNodeStates(uncheckedNode, Allocator.Temp);
                uncheckedStates.Sub(ref currentStates);
                
                //为了避免没有state的node(例如wander)与startNode有相同的hash，这种node被强制给了一个空state
                //因此在只有1个state且内容为空时，也应视为找到了plan
                if (uncheckedStates.Length() <= 0 ||
                    (uncheckedStates.Length()==1 && uncheckedStates[0].Equals(default)))
                {
                    //找到Plan，追加起点Node
                    Debugger?.Log("found plan: "+uncheckedNode.Name);
                    nodeGraph.LinkStartNode(uncheckedNode, new NativeString64("start"));
                    foundPlan = true;
                    //todo Early Exit
                }
                
                //检查uncheckedNodes的parent是否已经存在于其children之中
                //如果出现这种情况说明产生了循环，移去新得到的edge
                //并且不不把此uncheckedNode加入待展开列表
                var loop = false;
                var children = nodeGraph.GetChildren(uncheckedNode);
                if (children.Count > 0)
                {
                    var edges = nodeGraph.GetEdgeToParents(uncheckedNode);
                    while (edges.MoveNext())
                    {
                        if (!children.Contains(edges.Current.Parent)) continue;
                        loop = true;
                        nodeGraph.RemoveEdge(uncheckedNode, edges.Current.Parent);
                        break;
                    }
                }
                
                if(!loop)unexpandedNodes.Add(uncheckedNode);

                uncheckedStates.Dispose();
            }

            uncheckedNodes.Clear();
            return foundPlan;
        }

        public void ExpandNodes(ref NativeList<Node> unexpandedNodes, ref StackData stackData,
            ref NodeGraph nodeGraph, ref NativeList<Node> uncheckedNodes, ref NativeList<Node> expandedNodes,
            int iteration)
        {
            if (unexpandedNodes.Length <= 0) return;
            
            foreach (var node in unexpandedNodes)
            {
                Debugger?.Log("expanding node: "+node.Name+", "+node.GetHashCode());
            }
            var newlyCreatedNodes = new NativeQueue<Node>(Allocator.TempJob);
            var newlyCreatedNodesWriter = newlyCreatedNodes.AsParallelWriter();

            var existedNodesHash = nodeGraph.GetAllNodesHash(Allocator.TempJob);
            var nodeStates = nodeGraph.GetNodeStates(ref unexpandedNodes, Allocator.TempJob);
            var nodeToParentWriter = nodeGraph.NodeToParentWriter;
            var nodeStateWriter = nodeGraph.NodeStateWriter;
            var preconditionWriter = nodeGraph.PreconditionWriter;
            var effectWriter = nodeGraph.EffectWriter;
            
            var entityManager = World.Active.EntityManager;
            var handle = default(JobHandle);
            handle = ScheduleActionExpand<DropItemAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStates,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<PickItemAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStates,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<EatAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStates,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<CookAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStates,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<WanderAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<CollectAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<PickRawAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<DropRawAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref newlyCreatedNodesWriter, iteration);

            handle.Complete();
            existedNodesHash.Dispose();
            nodeStates.Dispose();
            
            expandedNodes.AddRange(unexpandedNodes);
            unexpandedNodes.Clear();
            while (newlyCreatedNodes.Count>0)
            {
                var node = newlyCreatedNodes.Dequeue();
                uncheckedNodes.Add(node);
            }
            newlyCreatedNodes.Dispose();
        }
        
        private JobHandle ScheduleActionExpand<T>(JobHandle handle, EntityManager entityManager,
            ref StackData stackData, ref NativeList<Node> unexpandedNodes,
            ref NativeArray<int> existedNodesHash, ref NativeMultiHashMap<Node, State>  nodeStates,
            NativeMultiHashMap<Node, Edge>.ParallelWriter nodeToParentWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter effectWriter,
            ref NativeQueue<Node>.ParallelWriter newlyCreatedNodesWriter, int iteration) where T : struct, IAction
        {
            for (var i = 0; i < stackData.AgentEntities.Length; i++)
            {
                stackData.CurrentAgentId = i;
                var agentEntity = stackData.AgentEntities[i];
                if (entityManager.HasComponent<T>(agentEntity))
                {
                    handle = new ActionExpandJob<T>(ref unexpandedNodes, ref existedNodesHash,
                        ref stackData, ref nodeStates,
                        nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                        ref newlyCreatedNodesWriter, iteration, new T()).Schedule(
                        unexpandedNodes, 6, handle);
                }
            }
            return handle;
        }
    }
}