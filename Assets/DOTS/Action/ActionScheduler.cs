using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.Action
{
    public class ActionScheduler
    {
        // Input
        [ReadOnly]
        public NativeList<Node> UnexpandedNodes;

        [ReadOnly]
        public StackData StackData;
        
        //Output
        public NodeGraph NodeGraph;

        public NativeList<Node> NewlyExpandedNodes;

        public int Iteration;

        [BurstCompile]
        private struct ActionExpandJob<T> : IJobParallelForDefer where T : struct, IAction
        {
            [ReadOnly]
            private NativeList<Node> _unexpandedNodes;

            [ReadOnly]
            private StackData _stackData;

            private NodeGraph _nodeGraph;

            [NativeDisableParallelForRestriction]
            private NativeList<Node> _newlyExpandedNodes;

            private readonly int _iteration;

            private T _action;

            public ActionExpandJob(ref NativeList<Node> unexpandedNodes, ref StackData stackData,
                ref NodeGraph nodeGraph, ref NativeList<Node> newlyExpandedNodes, int iteration, T action)
            {
                _unexpandedNodes = unexpandedNodes;
                _stackData = stackData;
                _nodeGraph = nodeGraph;
                _newlyExpandedNodes = newlyExpandedNodes;
                _iteration = iteration;
                _action = action;
            }

            public void Execute(int jobIndex)
            {
                var unexpandedNode = _unexpandedNodes[jobIndex];
                var targetStates = _nodeGraph.GetNodeStates(unexpandedNode, Allocator.Temp);

                var preconditions = new StateGroup(1, Allocator.Temp);
                var effects = new StateGroup(1, Allocator.Temp);

                var targetState = _action.GetTargetGoalState(ref targetStates, ref _stackData);

                if (!targetState.Equals(State.Null))
                {
                    _action.GetPreconditions(ref targetState, ref _stackData, ref preconditions);
                    _action.GetEffects(ref targetState, ref _stackData, ref effects);

                    if (effects.Length() > 0)
                    {
                        var newStates = new StateGroup(targetStates, Allocator.Temp);
                        newStates.Sub(effects);
                        newStates.Merge(preconditions);

                        var node = new Node(ref newStates, _action.GetName(), _iteration,
                            _action.GetNavigatingSubject(ref targetState, ref _stackData, ref preconditions));

                        //NodeGraph的几个容器都移去了并行限制，小心出错
                        _nodeGraph.AddRouteNode(node, ref newStates, ref preconditions, ref effects,
                            unexpandedNode, _action.GetName());
                        _newlyExpandedNodes.Add(node);

                        newStates.Dispose();
                    }
                }

                preconditions.Dispose();
                effects.Dispose();
                targetStates.Dispose();
            }
        }

        public JobHandle Schedule(JobHandle inputDeps)
        {
            var entityManager = World.Active.EntityManager;
            
            if (entityManager.HasComponent<DropItemAction>(StackData.AgentEntity))
            {
                inputDeps = new ActionExpandJob<DropItemAction>(ref UnexpandedNodes, ref StackData,
                    ref NodeGraph, ref NewlyExpandedNodes, Iteration, new DropItemAction()).Schedule(
                    UnexpandedNodes, 0, inputDeps);
            }

            if (entityManager.HasComponent<PickItemAction>(StackData.AgentEntity))
            {
                inputDeps = new ActionExpandJob<PickItemAction>(ref UnexpandedNodes, ref StackData,
                    ref NodeGraph, ref NewlyExpandedNodes, Iteration, new PickItemAction()).Schedule(
                    UnexpandedNodes, 0, inputDeps);
            }
            
            if (entityManager.HasComponent<EatAction>(StackData.AgentEntity))
            {
                inputDeps = new ActionExpandJob<EatAction>(ref UnexpandedNodes, ref StackData,
                    ref NodeGraph, ref NewlyExpandedNodes, Iteration, new EatAction()).Schedule(
                    UnexpandedNodes, 0, inputDeps);
            }

            return inputDeps;
        }
    }
}