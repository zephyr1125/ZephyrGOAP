using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Debugger;
using DOTS.Struct;
using DOTS.System.GoapPlanningJob;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SensorSystemGroup))]
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
        
        private EntityQuery _agentQuery, _currentStateQuery;

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
            _currentStateQuery = GetEntityQuery(
                ComponentType.ReadOnly<CurrentStates>(),
                ComponentType.ReadOnly<State>());
        }

        protected override void OnUpdate()
        {
            //todo 首先，应有goal挑选系统已经把goal分配到了各个agent身上，以及goal states也以buffer形式存于agent身上
            //SensorSystemGroup提前做好CurrentState的准备
            
            var agentEntities = _agentQuery.ToEntityArray(Allocator.TempJob);

            //从currentState的存储Entity上拿取current states
            var currentStatesEntities = _currentStateQuery.ToEntityArray(Allocator.TempJob);
            var currentStateBuffer = EntityManager.GetBuffer<State>(currentStatesEntities[0]);
            var stackData = new StackData
            {
                CurrentStates = new StateGroup(ref currentStateBuffer, Allocator.TempJob)
            };
            
            foreach (var agentEntity in agentEntities)
            {
                Debugger?.StartLog(EntityManager, agentEntity);
                
                var foundPlan = false;
                var goalStatesBuffer = EntityManager.GetBuffer<State>(agentEntity);
                var goalStates = new StateGroup(ref goalStatesBuffer, Allocator.Temp);

                stackData.AgentEntity = agentEntity;

                var uncheckedNodes = new NativeList<Node>(Allocator.TempJob);
                var unexpandedNodes = new NativeList<Node>(Allocator.TempJob);
                var expandedNodes = new NativeList<Node>(Allocator.TempJob);

                var nodeGraph = new NodeGraph(1, Allocator.TempJob);

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

                if (!foundPlan)
                {
                    //在展开阶段没有能够链接到current state的话，就没有找到规划，也就不用继续寻路了
                    //目前对于规划失败的情况，就直接转入NoGoal状态
                    Debugger?.Log("goal plan failed : "+goalStates);
                    goalStatesBuffer.Clear();
                    Utils.NextAgentState<GoalPlanning, NoGoal>(agentEntity, EntityManager, false);
                }
                else
                {
                    //寻路
                    //todo 此处每一个agent跑一次,寻路Job没有并行
                    //应该把各个agent的nodeGraph存一起，然后一起并行跑
                    FindPath(ref nodeGraph, agentEntity);
                    
                    //切换agent状态
                    Utils.NextAgentState<GoalPlanning, ReadyToNavigating>(agentEntity, EntityManager, false);
                }
                
                uncheckedNodes.Dispose();
                unexpandedNodes.Dispose();
                expandedNodes.Dispose();
                nodeGraph.Dispose();
                
            }

            agentEntities.Dispose();
            currentStatesEntities.Dispose();
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
            for (var i = 1; i < pathResult.Length; i++)    //path的0号为goal，不存
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
                    Debugger?.Log("add to expand: "+uncheckedNode.Name);
                    unexpandedNodes.Add(uncheckedNode);
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
            foreach (var node in unexpandedNodes)
            {
                Debugger?.Log("expanding node: "+node.Name+", "+node.GetHashCode());
            }
            var newlyExpandedNodes = new NativeList<Node>(Allocator.TempJob);
            
            var entityManager = World.Active.EntityManager;
            var handle = default(JobHandle);
            handle = ScheduleActionExpand<DropItemAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref nodeGraph, ref newlyExpandedNodes, iteration);
            handle = ScheduleActionExpand<PickItemAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref nodeGraph, ref newlyExpandedNodes, iteration);
            handle = ScheduleActionExpand<EatAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref nodeGraph, ref newlyExpandedNodes, iteration);
            handle = ScheduleActionExpand<CookAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref nodeGraph, ref newlyExpandedNodes, iteration);
            handle = ScheduleActionExpand<WanderAction>(handle, entityManager, ref stackData,
                ref unexpandedNodes, ref nodeGraph, ref newlyExpandedNodes, iteration);
            handle.Complete();
            
            foreach (var node in newlyExpandedNodes)
            {
                Debugger?.Log("create new node: "+node.Name+", "+node.GetHashCode());
            }
            
            expandedNodes.AddRange(unexpandedNodes);
            unexpandedNodes.Clear();
            uncheckedNodes.AddRange(newlyExpandedNodes);
            newlyExpandedNodes.Dispose();
        }
        
        private JobHandle ScheduleActionExpand<T>(JobHandle inputDeps, EntityManager entityManager,
            ref StackData stackData, ref NativeList<Node> unexpandedNodes, ref NodeGraph nodeGraph,
            ref NativeList<Node> newlyExpandedNodes, int iteration) where T : struct, IAction
        {
            if (entityManager.HasComponent<T>(stackData.AgentEntity))
            {
                inputDeps = new ActionExpandJob<T>(ref unexpandedNodes, ref stackData,
                    ref nodeGraph, ref newlyExpandedNodes, iteration, new T()).Schedule(
                    unexpandedNodes, 0, inputDeps);
            }

            return inputDeps;
        }
    }
}