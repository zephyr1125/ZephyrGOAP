using System;
using Unity.Assertions;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.Trait;
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
        private NativeList<int> _nodeStateIndices;
        [ReadOnly]
        private NativeList<State> _nodeStates;
        
        /// <summary>
        /// NodeGraph中现存所有Node的hash
        /// </summary>
        [ReadOnly]
        private NativeArray<int> _existedNodesHash;

        private NativeHashMap<int, Node>.ParallelWriter _nodesWriter;
        
        private NativeList<int>.ParallelWriter _nodeToParentIndicesWriter;
        private NativeList<int>.ParallelWriter _nodeToParentsWriter;
        
        private NativeList<int>.ParallelWriter _nodeStateIndicesWriter;
        private NativeList<State>.ParallelWriter _nodeStatesWriter;
        
        private NativeList<int>.ParallelWriter _preconditionIndicesWriter;
        private NativeList<State>.ParallelWriter _preconditionsWriter;
        
        private NativeList<ValueTuple<int, State>>.ParallelWriter _effectsWriter;
        
        private NativeHashMap<int, Node>.ParallelWriter _newlyCreatedNodesWriter;

        private readonly int _iteration;

        private T _action;

        public ActionExpandJob(ref NativeList<Node> unexpandedNodes, 
            ref NativeArray<int> existedNodesHash, ref StackData stackData,
            ref NativeList<int> nodeStateIndices, ref NativeList<State> nodeStates,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeList<int>.ParallelWriter nodeToParentIndicesWriter,
            NativeList<int>.ParallelWriter nodeToParentsWriter,
            NativeList<int>.ParallelWriter nodeStateIndicesWriter,
            NativeList<State>.ParallelWriter nodeStatesWriter, 
            NativeList<int>.ParallelWriter preconditionIndicesWriter,
            NativeList<State>.ParallelWriter preconditionsWriter, 
            NativeList<ValueTuple<int, State>>.ParallelWriter effectsWriter, 
            ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration, T action)
        {
            _unexpandedNodes = unexpandedNodes;
            _existedNodesHash = existedNodesHash;
            _stackData = stackData;
            _nodeStateIndices = nodeStateIndices;
            _nodeStates = nodeStates;
            _nodesWriter = nodesWriter;
            _nodeToParentIndicesWriter = nodeToParentIndicesWriter;
            _nodeToParentsWriter = nodeToParentsWriter;
            _nodeStateIndicesWriter = nodeStateIndicesWriter;
            _nodeStatesWriter = nodeStatesWriter;
            _preconditionIndicesWriter = preconditionIndicesWriter;
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
            for (var i = 0; i < _nodeStateIndices.Length; i++)
            {
                if (!_nodeStateIndices[i].Equals(unexpandedNode.HashCode)) continue;
                var state = _nodeStates[i];
                var priority = state.Target.Index;
                sortedStates.Add(new MinHashNode<State>(_nodeStates[i], priority));
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
                    if(preconditions.Length()==0)preconditions.Add(new State());

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
            Node parent, NativeString64 actionName)
        {
            newNode.Name = actionName;
            
            _nodeToParentIndicesWriter.AddNoResize(newNode.HashCode);
            _nodeToParentsWriter.AddNoResize(parent.HashCode);
            
            if(!nodeExisted)
            {
                _nodesWriter.TryAdd(newNode.HashCode, newNode);
                
                for(var i=0; i<nodeStates.Length(); i++)
                {
                    var state = nodeStates[i];
                    _nodeStateIndicesWriter.AddNoResize(newNode.HashCode);
                    _nodeStatesWriter.AddNoResize(state);
                }
                
                if(!preconditions.Equals(default(StateGroup)))
                {
                    for(var i=0; i<preconditions.Length(); i++)
                    {
                        var state = preconditions[i];
                        _preconditionIndicesWriter.AddNoResize(newNode.HashCode);
                        _preconditionsWriter.AddNoResize(state);
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
                        if (!state.Trait.Equals(typeof(ItemSourceTrait))) continue;
                        if (!state.ValueString.Equals("feast") &&
                            !state.ValueString.Equals("roast_apple")) continue;
                        Debug.Log($"{_iteration}({baseNode.HashCode}->{newNode.HashCode}){newNode.AgentExecutorEntity}|{state.ValueString}");
                    }
                }
            }
        }
    }
}