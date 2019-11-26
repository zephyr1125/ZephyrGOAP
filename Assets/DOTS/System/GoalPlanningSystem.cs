using DOTS.ActionJob;
using DOTS.Component;
using DOTS.Component.Actions;
using DOTS.Debugger;
using DOTS.Struct;
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
        public int Iterations = 10;
        
        private EntityQuery _agentQuery, _currentStateQuery;

        public IGoapDebugger Debugger;

        protected override void OnCreate()
        {
            base.OnCreate();
            _agentQuery = GetEntityQuery(
                ComponentType.ReadOnly<Agent>(),
                ComponentType.ReadOnly<Action>(),
                ComponentType.ReadOnly<PlanningGoal>(),
                ComponentType.ReadOnly<State>());
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
            
            for (var i = 0; i < agentEntities.Length; i++)
            {
                var iteration = 0;

                var agentEntity = agentEntities[i];
                var goalStatesBuffer = EntityManager.GetBuffer<State>(agentEntity);
                var goalStates = new StateGroup(ref goalStatesBuffer, Allocator.Temp);

                stackData.AgentEntity = agentEntity;

                var uncheckedNodes = new NativeList<Node>(Allocator.TempJob);
                var unexpandedNodes = new NativeList<Node>(Allocator.TempJob);
                var expandedNodes = new NativeList<Node>(Allocator.TempJob);

                var nodeGraph = new NodeGraph(1, Allocator.TempJob);

                var goalNode = new Node(ref goalStates, "goal");
                //goalNode进入graph
                nodeGraph.SetGoalNode(goalNode, ref goalStates);

                //goalNode进入待检查列表
                uncheckedNodes.Add(goalNode);

                while (uncheckedNodes.Length > 0 && iteration < Iterations)
                {
                    Debugger?.Log("Loop:");
                    //对待检查列表进行检查（与CurrentStates比对）
                    CheckNodes(ref uncheckedNodes, ref nodeGraph, ref stackData.CurrentStates,
                        ref unexpandedNodes);

                    //对待展开列表进行展开，并挑选进入待检查和展开后列表
                    ExpandNodes(ref unexpandedNodes, ref stackData, ref nodeGraph,
                        ref uncheckedNodes, ref expandedNodes);
                    //直至待展开列表为空或Early Exit
                    iteration++;
                }

                Debugger?.SetNodeGraph(ref nodeGraph);

                uncheckedNodes.Dispose();
                unexpandedNodes.Dispose();
                expandedNodes.Dispose();
                nodeGraph.Dispose();
            }

            agentEntities.Dispose();
            currentStatesEntities.Dispose();
            stackData.Dispose();
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
        public void CheckNodes(ref NativeList<Node> uncheckedNodes, ref NodeGraph nodeGraph,
            ref StateGroup currentStates, ref NativeList<Node> unexpandedNodes)
        {
            foreach (var uncheckedNode in uncheckedNodes)
            {
                Debugger?.Log("check node: "+uncheckedNode.Name);
                var uncheckedStates = nodeGraph.GetStateGroup(uncheckedNode, Allocator.Temp);
                uncheckedStates.Sub(currentStates);
                if (uncheckedStates.Length() <= 0)
                {
                    //找到Plan，追加起点Node
                    Debugger?.Log("found plan: "+uncheckedNode.Name);
                    nodeGraph.LinkStartNode(uncheckedNode, new NativeString64("start"));
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
        }

        public void ExpandNodes(ref NativeList<Node> unexpandedNodes, ref StackData stackData,
            ref NodeGraph nodeGraph, ref NativeList<Node> uncheckedNodes, ref NativeList<Node> expandedNodes)
        {
            foreach (var node in unexpandedNodes)
            {
                Debugger?.Log("expanding node: "+node.Name);
            }
            var newlyExpandedNodes = new NativeList<Node>(Allocator.TempJob);
            var actionScheduler = new ActionScheduler
            {
                UnexpandedNodes = unexpandedNodes,
                StackData = stackData,
                NodeGraph = nodeGraph,
                NewlyExpandedNodes = newlyExpandedNodes
            };
            var handle = actionScheduler.Schedule(default);
            handle.Complete();
            
            foreach (var node in newlyExpandedNodes)
            {
                Debugger?.Log("create new node: "+node.Name);
            }
            
            expandedNodes.AddRange(unexpandedNodes);
            unexpandedNodes.Clear();
            uncheckedNodes.AddRange(newlyExpandedNodes);
            newlyExpandedNodes.Dispose();
        }
    }
}