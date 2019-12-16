using DOTS.Action;
using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System.GoapPlanningJob
{
    [BurstCompile]
    public struct ActionExpandJob<T> : IJobParallelForDefer where T : struct, IAction
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
                ReplacePreconditionsWithSpecificStates(ref preconditions);
                
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

        /// <summary>
        /// 把preconditions里能够找到具体目标的范围state用具体目标替代
        /// </summary>
        /// <param name="preconditions"></param>
        private void ReplacePreconditionsWithSpecificStates(ref StateGroup preconditions)
        {
            for (var i = 0; i < preconditions.Length(); i++)
            {
                if (preconditions[i].Target != Entity.Null) continue;
                foreach (var currentState in _stackData.CurrentStates)
                {
                    //todo 此处应寻找最近目标
                    if (currentState.BelongTo(preconditions[i]))
                    {
                        preconditions[i] = currentState;
                    }
                }
            }
        }
    }
}