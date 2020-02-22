using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Debugger;
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
        public int ExpandIterations = 10;

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
                    ComponentType.ReadOnly<GoalPlanning>(),
                    ComponentType.ReadOnly<State>()
                },
                None = new []
                {
                    ComponentType.ReadOnly<Node>(), 
                }
            });
            _goalQuery = GetEntityQuery(
                ComponentType.ReadOnly<Goal>(),
                ComponentType.ReadOnly<PlanningGoal>());
        }

        protected override void OnUpdate()
        {
            //todo 首先，应有goal挑选系统已经把goal分配到了各个agent身上，以及goal states也以buffer形式存于agent身上
            //SensorSystemGroup提前做好CurrentState的准备
            
            var agentEntities = _agentQuery.ToEntityArray(Allocator.TempJob);
            if (agentEntities.Length <= 0)
            {
                agentEntities.Dispose();
                return;
            }

            var goalEntities = _goalQuery.ToEntityArray(Allocator.TempJob);
            var planningGoals = _goalQuery.ToComponentDataArray<PlanningGoal>(Allocator.TempJob);

            //从currentState的存储Entity上拿取current states
            var currentStateBuffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            var stackData = new StackData
            {
                CurrentStates = new StateGroup(ref currentStateBuffer, Allocator.TempJob)
            };
            
            foreach (var agentEntity in agentEntities)
            {
                Debugger?.StartLog(EntityManager, agentEntity);
                Debugger?.SetCurrentStates(ref stackData.CurrentStates, EntityManager);
                
                var foundPlan = false;
                var goalStatesBuffer = EntityManager.GetBuffer<State>(agentEntity);
                var goalStates = new StateGroup(ref goalStatesBuffer, Allocator.Temp);

                //找到goalEntity
                var goalEntity = Entity.Null;
                for (var i = 0; i < planningGoals.Length; i++)
                {
                    var planningGoal = planningGoals[i];
                    if (!planningGoal.AgentEntity.Equals(agentEntity)) continue;
                    goalEntity = goalEntities[i];
                    break;
                }

                stackData.AgentEntity = agentEntity;
                stackData.AgentPosition =
                    EntityManager.GetComponentData<Translation>(agentEntity).Value;

                var uncheckedNodes = new NativeList<Node>(Allocator.TempJob);
                var unexpandedNodes = new NativeList<Node>(Allocator.TempJob);
                var expandedNodes = new NativeList<Node>(Allocator.TempJob);

                var nodeGraph = new NodeGraph(512, Allocator.TempJob);

                var goalNode = new Node(ref goalStates, new NativeString64("goal"), 0, 0);
                //goalNode进入graph
                nodeGraph.SetGoalNode(goalNode, ref goalStates);

                //goalNode进入待检查列表
                uncheckedNodes.Add(goalNode);

                var iteration = 1;    //goal node iteration is 0
                
                while (uncheckedNodes.Length > 0 && iteration < ExpandIterations)
                {
                    Debugger?.Log("Loop:");
                    //对待检查列表进行检查（与CurrentStates比对）
                    if(CheckNodes(ref uncheckedNodes, ref nodeGraph, ref stackData.CurrentStates,
                        ref unexpandedNodes))foundPlan = true;

                    //对待展开列表进行展开，并挑选进入待检查和展开后列表
                    ExpandNodes(ref unexpandedNodes, ref stackData, ref nodeGraph,
                        ref uncheckedNodes, ref expandedNodes, iteration);

                    //直至待展开列表为空或Early Exit
                    iteration++;
                }

                Debugger?.SetNodeGraph(ref nodeGraph, EntityManager);

                EntityManager.RemoveComponent<PlanningGoal>(goalEntity);
                if (!foundPlan)
                {
                    //在展开阶段没有能够链接到current state的话，就没有找到规划，也就不用继续寻路了
                    //目前对于规划失败的情况，就直接转入NoGoal状态
                    Debugger?.Log("goal plan failed : "+goalStates);

                    EntityManager.AddComponentData(goalEntity,
                        new PlanFailedGoal{AgentEntity = agentEntity, FailTime = Time.ElapsedTime});
                    
                    EntityManager.GetBuffer<State>(agentEntity).Clear();
                    Utils.NextAgentState<GoalPlanning, NoGoal>(agentEntity, EntityManager, false);
                }
                else
                {
                    //寻路
                    //todo 此处每一个agent跑一次,寻路Job没有并行
                    //应该把各个agent的nodeGraph存一起，然后一起并行跑
                    FindPath(ref nodeGraph, agentEntity);
                    
                    EntityManager.AddComponentData(goalEntity,
                        new ExecutingGoal{AgentEntity = agentEntity});
                    
                    //切换agent状态
                    Utils.NextAgentState<GoalPlanning, ReadyToNavigate>(agentEntity, EntityManager, false);
                }
                
                uncheckedNodes.Dispose();
                unexpandedNodes.Dispose();
                expandedNodes.Dispose();
                nodeGraph.Dispose();
                
                Debugger?.LogDone();
            }

            planningGoals.Dispose();
            goalEntities.Dispose();
            agentEntities.Dispose();
            stackData.Dispose();
        }

        private void FindPath(ref NodeGraph nodeGraph, Entity agentEntity)
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

            Debugger?.SetPathResult(ref pathResult);

            //保存结果
            var nodeBuffer = EntityManager.AddBuffer<Node>(agentEntity);
            var stateBuffer =
                EntityManager.GetBuffer<State>(agentEntity); //已经在创建goal的时候创建了state buffer以容纳goal state
            for (var i = pathResult.Length-1; i > 0; i--)    //path的0号为goal，不存
            {
                var node = pathResult[i];
                var preconditions = nodeGraph.GetNodePreconditions(node, Allocator.Temp);
                var effects = nodeGraph.GetNodeEffects(node, Allocator.Temp);

                foreach (var precondition in preconditions)
                {
                    stateBuffer.Add(precondition);
                    node.PreconditionsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
                }

                foreach (var effect in effects)
                {
                    stateBuffer.Add(effect);
                    node.EffectsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
                }

                nodeBuffer.Add(node);

                preconditions.Dispose();
                effects.Dispose();
            }

            pathResult.Dispose();
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
                else
                {
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
                }

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
        
        private JobHandle ScheduleActionExpand<T>(JobHandle dependHandle, EntityManager entityManager,
            ref StackData stackData, ref NativeList<Node> unexpandedNodes,
            ref NativeArray<int> existedNodesHash, ref NativeMultiHashMap<Node, State>  nodeStates,
            NativeMultiHashMap<Node, Edge>.ParallelWriter nodeToParentWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter effectWriter,
            ref NativeQueue<Node>.ParallelWriter newlyCreatedNodesWriter, int iteration) where T : struct, IAction
        {
            if (entityManager.HasComponent<T>(stackData.AgentEntity))
            {
                dependHandle = new ActionExpandJob<T>(ref unexpandedNodes, ref existedNodesHash,
                    ref stackData, ref nodeStates,
                    nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                    ref newlyCreatedNodesWriter, iteration, new T()).Schedule(
                    unexpandedNodes, 0, dependHandle);
            }

            return dependHandle;
        }
    }
}