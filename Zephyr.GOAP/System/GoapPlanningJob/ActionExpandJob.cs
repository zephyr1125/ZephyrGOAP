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
        private NativeList<ValueTuple<int, State>> _requires;
        
        [ReadOnly]
        private NativeList<ValueTuple<int, State>> _deltas;
        
        /// <summary>
        /// NodeGraph中现存所有Node的hash
        /// </summary>
        [ReadOnly]
        private readonly NativeArray<int> _existedNodesHash;

        private NativeHashMap<int, Node>.ParallelWriter _nodesWriter;
        
        private NativeList<ValueTuple<int, int>>.ParallelWriter _nodeToParentsWriter;

        private NativeHashMap<int, State>.ParallelWriter _statesWriter;
        
        private NativeList<ValueTuple<int, int>>.ParallelWriter _preconditionHashesWriter;
        
        private NativeList<ValueTuple<int, int>>.ParallelWriter _effectHashesWriter;
        
        private NativeList<ValueTuple<int, int>>.ParallelWriter _requireHashesWriter;
        
        private NativeList<ValueTuple<int, int>>.ParallelWriter _deltaHashesWriter;
        
        private NativeHashMap<int, Node>.ParallelWriter _newlyCreatedNodesWriter;

        private readonly int _iteration;

        private T _action;

        public ActionExpandJob(ref NativeList<Node> unexpandedNodes, 
            ref NativeArray<int> existedNodesHash, ref StackData stackData,
            ref NativeList<ValueTuple<int, State>> requires,
            ref NativeList<ValueTuple<int, State>> deltas,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeList<ValueTuple<int, int>>.ParallelWriter nodeToParentsWriter,
            NativeHashMap<int, State>.ParallelWriter statesWriter,
            NativeList<ValueTuple<int, int>>.ParallelWriter preconditionHashesWriter, 
            NativeList<ValueTuple<int, int>>.ParallelWriter effectHashesWriter, 
            NativeList<ValueTuple<int, int>>.ParallelWriter requireHashesWriter, 
            NativeList<ValueTuple<int, int>>.ParallelWriter deltaHashesWriter, 
            ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration, T action)
        {
            _unexpandedNodes = unexpandedNodes;
            _existedNodesHash = existedNodesHash;
            _stackData = stackData;
            _requires = requires;
            _deltas = deltas;
            _nodesWriter = nodesWriter;
            _nodeToParentsWriter = nodeToParentsWriter;
            
            _statesWriter = statesWriter;
            _preconditionHashesWriter = preconditionHashesWriter;
            _effectHashesWriter = effectHashesWriter;
            _requireHashesWriter = requireHashesWriter;
            _deltaHashesWriter = deltaHashesWriter;
            
            _newlyCreatedNodesWriter = newlyCreatedNodesWriter;
            _iteration = iteration;
            _action = action;
        }

        public void Execute(int jobIndex)
        {
            var unexpandedNode = _unexpandedNodes[jobIndex];
            
            //只考虑node的首个require
            var sortedRequires = new ZephyrNativeMinHeap<State>(Allocator.Temp);
            for (var i = 0; i < _requires.Length; i++)
            {
                var (hash, state) = _requires[i];
                if (!hash.Equals(unexpandedNode.HashCode)) continue;
                var priority = state.Target.Index;
                sortedRequires.Add(new MinHashNode<State>(state, priority));
            }
            var leftRequires = new StateGroup(sortedRequires, Allocator.Temp);
            var targetRequires = new StateGroup(leftRequires, 1, Allocator.Temp);
            sortedRequires.Dispose();
            
            var targetRequire = _action.GetTargetRequire(ref targetRequires, ref _stackData);
            targetRequires.Dispose();

            if (!targetRequire.Equals(State.Null))
            {
                //提取属于本node的deltas
                var deltas = new StateGroup(2, Allocator.Temp);
                for (var i = 0; i < _deltas.Length; i++)
                {
                    var (hash, state) = _deltas[i];
                    if (!hash.Equals(unexpandedNode.HashCode)) continue;
                    deltas.Add(state);
                }
                
                var settings = _action.GetSettings(ref targetRequire, ref _stackData, Allocator.Temp);

                for (var i=0; i<settings.Length(); i++)
                {
                    var setting = settings[i];
                    var preconditions = new StateGroup(1, Allocator.Temp);
                    var effects = new StateGroup(1, Allocator.Temp);

                    _action.GetPreconditions(ref targetRequire, ref setting, ref _stackData, ref preconditions);

                    _action.GetEffects(ref targetRequire, ref setting, ref _stackData, ref effects);

                    if (effects.Length() > 0)
                    {
                        var requires = new StateGroup(leftRequires, Allocator.Temp);
                        requires.AND(effects);
                        requires.OR(preconditions);

                        var reward =
                            _action.GetReward(ref targetRequire, ref setting, ref _stackData);
                        
                        var time =
                            _action.GetExecuteTime(ref targetRequire, ref setting, ref _stackData);

                        _action.GetNavigatingSubjectInfo(ref targetRequire, ref setting,
                            ref _stackData, ref preconditions, out var subjectType, out var subjectId);
                        
                        var node = new Node(ref preconditions, ref effects, ref requires, ref deltas,
                            _action.GetName(), reward, time, _iteration,
                            _stackData.AgentEntities[_stackData.CurrentAgentId], subjectType, subjectId);

                        var nodeExisted = _existedNodesHash.Contains(node.HashCode);
                        
                        AddRouteNode(unexpandedNode, node, nodeExisted, 
                            ref preconditions, ref effects, ref requires, ref deltas,
                            unexpandedNode, _action.GetName());
                        _newlyCreatedNodesWriter.TryAdd(node.HashCode, node);

                        requires.Dispose();
                    }
                
                    preconditions.Dispose();
                    effects.Dispose();
                }
                settings.Dispose();
                deltas.Dispose();
            }
            leftRequires.Dispose();
        }

        /// <summary>
        /// <param name="newNode"></param>
        /// <param name="requires"></param>
        /// <param name="preconditions"></param>
        /// <param name="effects"></param>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        /// <returns>此node已存在</returns>
        /// </summary>
        private void AddRouteNode(Node baseNode, Node newNode, bool nodeExisted,
            ref StateGroup preconditions, ref StateGroup effects, ref StateGroup requires, ref StateGroup deltas,
            Node parent, NativeString32 actionName)
        {
            newNode.Name = actionName;
            
            _nodeToParentsWriter.AddNoResize((newNode.HashCode, parent.HashCode));
            
            if(!nodeExisted)
            {
                _nodesWriter.TryAdd(newNode.HashCode, newNode);
                
                if(!preconditions.Equals(default))
                {
                    for(var i=0; i<preconditions.Length(); i++)
                    {
                        var state = preconditions[i];
                        var stateHash = state.GetHashCode();
                        _statesWriter.TryAdd(stateHash, state);
                        _preconditionHashesWriter.AddNoResize((newNode.HashCode, stateHash));
                    }
                }

                if (!effects.Equals(default))
                {
                    //目前effect不可能超过1个
                    Assert.IsTrue(effects.Length()<2, "[AddRouteNode] Too much effects!");
                    for(var i=0; i<effects.Length(); i++)
                    {
                        var state = effects[i];
                        var stateHash = state.GetHashCode();
                        _statesWriter.TryAdd(stateHash, state);
                        _effectHashesWriter.AddNoResize((newNode.HashCode, stateHash));
                    }
                }
                
                for(var i=0; i<requires.Length(); i++)
                {
                    var state = requires[i];
                    var stateHash = state.GetHashCode();
                    _statesWriter.TryAdd(stateHash, state);
                    _requireHashesWriter.AddNoResize((newNode.HashCode, stateHash));
                }
                
                if (!deltas.Equals(default))
                {
                    for(var i=0; i<deltas.Length(); i++)
                    {
                        var state = deltas[i];
                        var stateHash = state.GetHashCode();
                        _statesWriter.TryAdd(stateHash, state);
                        _deltaHashesWriter.AddNoResize((newNode.HashCode, stateHash));
                    }
                }
            }
        }
    }
}