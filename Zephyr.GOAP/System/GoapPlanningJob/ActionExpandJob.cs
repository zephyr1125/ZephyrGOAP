using System;
using Unity.Assertions;
using Unity.Collections;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoapPlanningJob
{
    // [BurstCompile]
    public struct ActionExpandJob<T> : IJobParallelForDefer where T : struct, IAction
    {
        [ReadOnly]
        private NativeList<Node> _unexpandedNodes;

        [ReadOnly]
        private StackData _stackData;

        [ReadOnly]
        private NativeList<ValueTuple<int, State>> _nodeStates;
        
        /// <summary>
        /// NodeGraph中现存所有Node的hash
        /// </summary>
        [ReadOnly]
        private NativeArray<int> _existedNodesHash;

        private NativeHashMap<int, Node>.ParallelWriter _nodesWriter;
        
        private NativeList<ValueTuple<int, int>>.ParallelWriter _nodeToParentsWriter;
        
        private NativeList<ValueTuple<int, State>>.ParallelWriter _nodeStatesWriter;
        
        private NativeList<ValueTuple<int, State>>.ParallelWriter _preconditionsWriter;
        
        private NativeList<ValueTuple<int, State>>.ParallelWriter _effectsWriter;
        
        private NativeHashMap<int, Node>.ParallelWriter _newlyCreatedNodesWriter;

        private readonly int _iteration;

        private T _action;

        public ActionExpandJob(ref NativeList<Node> unexpandedNodes, 
            ref NativeArray<int> existedNodesHash, ref StackData stackData,
            ref NativeList<ValueTuple<int, State>> nodeStates,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeList<ValueTuple<int, int>>.ParallelWriter nodeToParentsWriter,
            NativeList<ValueTuple<int, State>>.ParallelWriter nodeStatesWriter, 
            NativeList<ValueTuple<int, State>>.ParallelWriter preconditionsWriter, 
            NativeList<ValueTuple<int, State>>.ParallelWriter effectsWriter, 
            ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration, T action)
        {
            _unexpandedNodes = unexpandedNodes;
            _existedNodesHash = existedNodesHash;
            _stackData = stackData;
            _nodeStates = nodeStates;
            _nodesWriter = nodesWriter;
            _nodeToParentsWriter = nodeToParentsWriter;
            _nodeStatesWriter = nodeStatesWriter;
            _preconditionsWriter = preconditionsWriter;
            _effectsWriter = effectsWriter;
            _newlyCreatedNodesWriter = newlyCreatedNodesWriter;
            _iteration = iteration;
            _action = action;
        }

        public void Execute(int jobIndex)
        {
            var unexpandedNode = _unexpandedNodes[jobIndex];
            //只考虑node的首个state
            var sortedStates = new ZephyrNativeMinHeap<State>(Allocator.Temp);
            for (var i = 0; i < _nodeStates.Length; i++)
            {
                var (hash, state) = _nodeStates[i];
                if (!hash.Equals(unexpandedNode.HashCode)) continue;
                var priority = state.Target.Index;
                sortedStates.Add(new MinHashNode<State>(state, priority));
            }
            var leftStates = new StateGroup(sortedStates, Allocator.Temp);
            var targetStates = new StateGroup(leftStates, 1, Allocator.Temp);
            sortedStates.Dispose();
            
            var targetState = _action.GetTargetGoalState(ref targetStates, ref _stackData);
            targetStates.Dispose();

            if (!targetState.Equals(State.Null))
            {
                var settings = _action.GetSettings(ref targetState, ref _stackData, Allocator.Temp);

                for (var i=0; i<settings.Length(); i++)
                {
                    var setting = settings[i];
                    var preconditions = new StateGroup(1, Allocator.Temp);
                    var effects = new StateGroup(1, Allocator.Temp);

                    _action.GetPreconditions(ref targetState, ref setting, ref _stackData, ref preconditions);
                    //为了避免没有state的node(例如wander)与startNode有相同的hash，这种node被强制给了一个空state
                    // if(preconditions.Length()==0)preconditions.Add(new State());

                    _action.GetEffects(ref targetState, ref setting, ref _stackData, ref effects);

                    if (effects.Length() > 0)
                    {
                        var newStates = new StateGroup(leftStates, Allocator.Temp);
                        newStates.SubForEffect(ref effects);
                        newStates.Merge(preconditions);

                        var reward =
                            _action.GetReward(ref targetState, ref setting, ref _stackData);
                        
                        var time =
                            _action.GetExecuteTime(ref targetState, ref setting, ref _stackData);

                        _action.GetNavigatingSubjectInfo(ref targetState, ref setting,
                            ref _stackData, ref preconditions, out var subjectType, out var subjectId);
                        
                        var node = new Node(ref preconditions, ref effects, ref newStates, 
                            _action.GetName(), reward, time, _iteration,
                            _stackData.AgentEntities[_stackData.CurrentAgentId], subjectType, subjectId);

                        var nodeExisted = _existedNodesHash.Contains(node.HashCode);
                        
                        AddRouteNode(unexpandedNode, node, nodeExisted, ref newStates, 
                            ref preconditions, ref effects, unexpandedNode, _action.GetName());
                        _newlyCreatedNodesWriter.TryAdd(node.HashCode, node);

                        newStates.Dispose();
                    }
                
                    preconditions.Dispose();
                    effects.Dispose();
                }
                settings.Dispose();
            }
            leftStates.Dispose();
        }

        /// <summary>
        /// <param name="newNode"></param>
        /// <param name="nodeStates"></param>
        /// <param name="preconditions"></param>
        /// <param name="effects"></param>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        /// <returns>此node已存在</returns>
        /// </summary>
        private void AddRouteNode(Node baseNode, Node newNode, bool nodeExisted, ref StateGroup nodeStates,
            ref StateGroup preconditions, ref StateGroup effects,
            Node parent, NativeString32 actionName)
        {
            newNode.Name = actionName;
            
            _nodeToParentsWriter.AddNoResize((newNode.HashCode, parent.HashCode));
            
            if(!nodeExisted)
            {
                _nodesWriter.TryAdd(newNode.HashCode, newNode);
                
                for(var i=0; i<nodeStates.Length(); i++)
                {
                    var state = nodeStates[i];
                    _nodeStatesWriter.AddNoResize((newNode.HashCode, state));
                }
                
                if(!preconditions.Equals(default(StateGroup)))
                {
                    for(var i=0; i<preconditions.Length(); i++)
                    {
                        var state = preconditions[i];
                        _preconditionsWriter.AddNoResize((newNode.HashCode, state));
                    }
                }

                if (!effects.Equals(default(StateGroup)))
                {
                    //目前effect不可能超过1个
                    Assert.IsTrue(effects.Length()<2, "[AddRouteNode] Too much effects!");
                    for(var i=0; i<effects.Length(); i++)
                    {
                        var state = effects[i];
                        _effectsWriter.AddNoResize((newNode.HashCode, state));

                        // if (!newNode.Name.Equals("CookAction")) continue;
                        // if (!state.Trait.Equals(typeof(ItemSourceTrait))) continue;
                        // if (!state.ValueString.Equals("feast") &&
                        //     !state.ValueString.Equals("roast_apple")) continue;
                        // Debug.Log($"{_iteration}({baseNode.HashCode}->{newNode.HashCode}){newNode.AgentExecutorEntity}|{state.ValueString}");
                    }
                }
            }
        }
    }
}