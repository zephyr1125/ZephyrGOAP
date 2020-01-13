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

        [ReadOnly]
        private NativeMultiHashMap<Node, State> _nodeStates;
        
        /// <summary>
        /// 与unexpandedNodes并齐，表示其是否已在graph中存在
        /// </summary>
        [ReadOnly]
        private NativeArray<bool> _nodesExisted;
        
        private NativeMultiHashMap<Node, Edge>.ParallelWriter _nodeToParentWriter;
        private NativeMultiHashMap<Node, State>.ParallelWriter _nodeStateWriter;
        private NativeMultiHashMap<Node, State>.ParallelWriter _preconditionWriter;
        private NativeMultiHashMap<Node, State>.ParallelWriter _effectWriter;

        [NativeDisableParallelForRestriction]
        private NativeList<Node> _newlyExpandedNodes;

        private readonly int _iteration;

        private T _action;

        public ActionExpandJob(ref NativeList<Node> unexpandedNodes, 
            ref NativeArray<bool> nodesExisted, ref StackData stackData,
            ref NativeMultiHashMap<Node, State> nodeStates,
            NativeMultiHashMap<Node, Edge>.ParallelWriter nodeToParentWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter effectWriter,
            ref NativeList<Node> newlyExpandedNodes, int iteration, T action)
        {
            _unexpandedNodes = unexpandedNodes;
            _nodesExisted = nodesExisted;
            _stackData = stackData;
            _nodeStates = nodeStates;
            _nodeToParentWriter = nodeToParentWriter;
            _nodeStateWriter = nodeStateWriter;
            _preconditionWriter = preconditionWriter;
            _effectWriter = effectWriter;
            _newlyExpandedNodes = newlyExpandedNodes;
            _iteration = iteration;
            _action = action;
        }

        public void Execute(int jobIndex)
        {
            var unexpandedNode = _unexpandedNodes[jobIndex];
            var nodeExisted = _nodesExisted[jobIndex];
            var targetStates = new StateGroup(3, _nodeStates.GetValuesForKey(unexpandedNode), Allocator.Temp);
            var targetState = _action.GetTargetGoalState(ref targetStates, ref _stackData);

            if (!targetState.Equals(State.Null))
            {
                var settings = _action.GetSettings(ref targetState, ref _stackData, Allocator.Temp);

                for (var i=0; i<settings.Length(); i++)
                {
                    var setting = settings[i];
                    var preconditions = new StateGroup(1, Allocator.Temp);
                    var effects = new StateGroup(1, Allocator.Temp);

                    _action.GetPreconditions(ref targetState, ref setting, ref _stackData, ref preconditions);
                    ReplacePreconditionsWithSpecificStates(ref preconditions);
                
                    _action.GetEffects(ref targetState, ref setting, ref _stackData, ref effects);

                    if (effects.Length() > 0)
                    {
                        var newStates = new StateGroup(targetStates, Allocator.Temp);
                        newStates.SubForEffect(ref effects);
                        newStates.Merge(preconditions);

                        var reward =
                            _action.GetReward(ref targetState, ref setting, ref _stackData);

                        var node = new Node(ref newStates, _action.GetName(), reward, _iteration,
                            _action.GetNavigatingSubject(ref targetState, ref setting, ref _stackData, ref preconditions));

                        //NodeGraph的几个容器都移去了并行限制，小心出错
                        AddRouteNode(node, nodeExisted, ref newStates, _nodeToParentWriter,
                            _nodeStateWriter, _preconditionWriter, _effectWriter,
                            ref preconditions, ref effects, unexpandedNode, _action.GetName());
                        _newlyExpandedNodes.Add(node);

                        newStates.Dispose();
                    }
                
                    preconditions.Dispose();
                    effects.Dispose();
                }
                settings.Dispose();
            }
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
        
        /// <summary>
        /// <param name="node"></param>
        /// <param name="nodeStates"></param>
        /// <param name="effectWriter"></param>
        /// <param name="preconditions"></param>
        /// <param name="effects"></param>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        /// <param name="nodeToParentWriter"></param>
        /// <param name="nodeStateWriter"></param>
        /// <param name="preconditionWriter"></param>
        /// <returns>此node已存在</returns>
        /// </summary>
        private void AddRouteNode(Node node, bool nodeExisted, ref StateGroup nodeStates,
            NativeMultiHashMap<Node, Edge>.ParallelWriter nodeToParentWriter,
            NativeMultiHashMap<Node, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter effectWriter,
            ref StateGroup preconditions, ref StateGroup effects,
            Node parent, NativeString64 actionName)
        {
            node.Name = actionName;
            
            nodeToParentWriter.Add(node, new Edge(parent, node, actionName));
            if(!nodeExisted){
                for(var i=0; i<nodeStates.Length(); i++)
                {
                    var state = nodeStates[i];
                    nodeStateWriter.Add(node, state);
                }
                
                if(!preconditions.Equals(default(StateGroup)))
                {
                    for(var i=0; i<preconditions.Length(); i++)
                    {
                        var state = preconditions[i];
                        preconditionWriter.Add(node, state);
                    }
                }

                if (!effects.Equals(default(StateGroup)))
                {
                    for(var i=0; i<effects.Length(); i++)
                    {
                        var state = effects[i];
                        effectWriter.Add(node, state);
                    }
                }
            }
        }

        private void AddRouteNode(Node node, bool nodeExisted, ref State nodeState,
            NativeMultiHashMap<Node, Edge>.ParallelWriter nodeToParentWriter,
            NativeMultiHashMap<Node, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter effectWriter,
            ref StateGroup preconditions, ref StateGroup effects,
            Node parent, NativeString64 actionName)
        {
            var stateGroup = new StateGroup(1, Allocator.Temp) {nodeState};
            AddRouteNode(node, nodeExisted, ref stateGroup,
                nodeToParentWriter, nodeStateWriter, preconditionWriter, effectWriter,
                ref preconditions, ref effects, parent, actionName);
            stateGroup.Dispose();
        }
    }
}