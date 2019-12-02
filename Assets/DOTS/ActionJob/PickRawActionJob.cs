using DOTS.Component.Actions;
using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.ActionJob
{
    [BurstCompile]
    public struct PickRawActionJob : IJobParallelForDefer
    {
        [ReadOnly]
        private NativeList<Node> _unexpandedNodes;

        [ReadOnly]
        private StackData _stackData;

        private NodeGraph _nodeGraph;

        [NativeDisableParallelForRestriction]
        private NativeList<Node> _newlyExpandedNodes;

        private readonly int _iteration;

        [ReadOnly]
        private DynamicBuffer<Action> _agentActions; 

        public PickRawActionJob(ref NativeList<Node> unexpandedNodes, ref StackData stackData,
            ref NodeGraph nodeGraph, ref NativeList<Node> newlyExpandedNodes, int iteration, ref DynamicBuffer<Action> agentActions)
        {
            _unexpandedNodes = unexpandedNodes;
            _stackData = stackData;
            _nodeGraph = nodeGraph;
            _newlyExpandedNodes = newlyExpandedNodes;
            _iteration = iteration;
            _agentActions = agentActions;
        }

        public void Execute(int jobIndex)
        {
            //没有本action的agent不运行
            var hasAction = false;
            foreach (var agentAction in _agentActions)
            {
                if (agentAction.ActionName.Equals(new NativeString64(nameof(PickRawActionJob))))
                {
                    hasAction = true;
                    break;
                }
            }
            if (!hasAction) return;
            
            var unexpandedNode = _unexpandedNodes[jobIndex];
            var targetStates = _nodeGraph.GetStateGroup(unexpandedNode, Allocator.Temp);
            
            var preconditions = new StateGroup(1, Allocator.Temp);
            var effects = new StateGroup(1, Allocator.Temp);

            var targetState = GetTargetGoalState(ref targetStates, ref _stackData);
            
            if (!targetState.Equals(default))
            {
                GetPreconditions(ref targetState, ref _stackData, ref preconditions);
                GetEffects(ref targetState, ref _stackData, ref effects);

                if (effects.Length() == 0) return;
            
                var newStates = new StateGroup(targetStates, Allocator.Temp);
                newStates.Sub(effects);
                newStates.Merge(preconditions);
            
                var node = new Node(ref newStates, nameof(PickRawActionJob), _iteration);
            
                //NodeGraph的几个容器都移去了并行限制，小心出错
                _nodeGraph.AddRouteNode(node, ref newStates, unexpandedNode,
                    new NativeString64(nameof(PickRawActionJob)));
                _newlyExpandedNodes.Add(node);
            
                newStates.Dispose();
            }
            
            preconditions.Dispose();
            effects.Dispose();
            targetStates.Dispose();
        }

        private State GetTargetGoalState([ReadOnly]ref StateGroup targetStates,
            [ReadOnly]ref StackData stackData)
        {
            foreach (var targetState in targetStates)
            {
                //只针对要求自身具有原料请求的goal state
                if (targetState.Target != stackData.AgentEntity) continue;
                if (targetState.Trait != typeof(RawTrait)) continue;

                return targetState;
            }

            return default;
        }
        
        /// <summary>
        /// 条件：世界里要有对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        private void GetPreconditions([ReadOnly]ref State targetState,
            [ReadOnly]ref StackData stackData, ref StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                SubjectType = StateSubjectType.Closest,
                Target = Entity.Null,
                Trait = typeof(RawTrait),
                Value = targetState.Value,
                IsPositive = true,
            });
        }

        /// <summary>
        /// 效果：自身获得对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        private void GetEffects([ReadOnly]ref State targetState,
            [ReadOnly]ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = stackData.AgentEntity,
                Trait = typeof(RawTrait),
                Value = targetState.Value,
                IsPositive = true
            });
        }
    }
}