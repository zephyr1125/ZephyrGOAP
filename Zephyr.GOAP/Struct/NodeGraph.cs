using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;

namespace Zephyr.GOAP.Struct
{
    public struct NodeGraph : IDisposable
    {
        [ReadOnly]
        private NativeHashMap<int, Node> _nodes;
        
        [ReadOnly]
        private NativeList<ValueTuple<int,int>> _nodeToParents;

        [ReadOnly]
        private NativeHashMap<int, State> _states;

        [ReadOnly]
        private NativeHashMap<int, int> _effectHashes;
        
        [ReadOnly]
        private NativeList<ValueTuple<int,State>> _nodeStates;
        
        [ReadOnly]
        private NativeList<ValueTuple<int,State>> _preconditions;
        
        [ReadOnly]
        private NativeList<ValueTuple<int,State>> _effects;

        public NativeList<int> _deadEndNodeHashes;

        /// <summary>
        /// 起点Node代表当前状态，没有Action
        /// </summary>
        public int StartNodeHash { get; }
        
        public int GoalNodeHash { get; private set; }

        public NodeGraph(int initialCapacity, ref DynamicBuffer<State> startNodeStates, Allocator allocator)
        {
            _nodes = new NativeHashMap<int, Node>(initialCapacity, allocator);
            
            _nodeToParents = new NativeList<ValueTuple<int, int>>(initialCapacity*4, allocator);
            _nodeStates = new NativeList<ValueTuple<int, State>>(initialCapacity*4, allocator);
            _preconditions = new NativeList<ValueTuple<int, State>>(initialCapacity*4, allocator);
            _effects = new NativeList<ValueTuple<int, State>>(initialCapacity*4, allocator);
            
            _states = new NativeHashMap<int, State>(initialCapacity*4, allocator);
            _effectHashes = new NativeHashMap<int, int>(initialCapacity*2, allocator);
            
            _deadEndNodeHashes = new NativeList<int>(allocator);
            
            var startNode = new Node {Name = "start"};
            StartNodeHash = startNode.HashCode;
            _nodes.Add(StartNodeHash, startNode);
            for (var i = 0; i < startNodeStates.Length; i++)
            {
                _effects.Add((StartNodeHash, startNodeStates[i]));
            }
            
            GoalNodeHash = 0;
        }

        public void SetGoalNode(Node goal, ref StateGroup stateGroup)
        {
            if (GoalNodeHash != 0)
            {
                _nodes.Remove(GoalNodeHash);
                for (var i = _nodeStates.Length - 1; i >= 0; i--)
                {
                    var (hash, _) = _nodeStates[i];
                    if (!hash.Equals(GoalNodeHash)) continue;
                    _nodeStates.RemoveAtSwapBack(i);
                }
            }
            
            GoalNodeHash = goal.HashCode;
            _nodes.Add(GoalNodeHash, goal);
            foreach (var state in stateGroup)
            {
                _nodeStates.Add((GoalNodeHash, state));
            }
        }

        public NativeHashMap<int, Node>.ParallelWriter NodesWriter => _nodes.AsParallelWriter();
        
        public NativeList<ValueTuple<int, int>>.ParallelWriter NodeToParentsWriter => _nodeToParents.AsParallelWriter();
        
        public NativeList<ValueTuple<int, State>>.ParallelWriter NodeStatesWriter => _nodeStates.AsParallelWriter();
        
        public NativeList<ValueTuple<int, State>>.ParallelWriter PreconditionsWriter => _preconditions.AsParallelWriter();
        
        public NativeList<ValueTuple<int, State>>.ParallelWriter EffectsWriter => _effects.AsParallelWriter();

        public NativeHashMap<int, State>.ParallelWriter StatesWriter => _states.AsParallelWriter();

        public NativeHashMap<int, int>.ParallelWriter EffectHashesWriter =>
            _effectHashes.AsParallelWriter();

        /// <summary>
        /// 追加对起点的链接
        /// </summary>
        /// <param name="parent"></param>
        public void LinkStartNode(Node parent)
        {
            //start node的iteration设置为此Node+1
            var iteration = parent.Iteration;
            var startNode = _nodes[StartNodeHash];
            if (startNode.Iteration <= iteration)
            {
                startNode.Iteration = iteration + 1;
                _nodes[StartNodeHash] = startNode;
            }
            _nodeToParents.Add((StartNodeHash, parent.HashCode));
        }

        public Node this[int hashCode]
        {
            get
            {
                return _nodes[hashCode];
            }
            set
            {
                Assert.IsTrue(_nodes.ContainsKey(hashCode), "You can't direct add node.");
                Assert.IsTrue(value.HashCode.Equals(hashCode), 
                    "You can't modify existed Node so that affected its HashCode");

                _nodes[hashCode] = value;
            }
        }

        /// <summary>
        /// 询问node的数量
        /// </summary>
        /// <returns></returns>
        public int Length()
        {
            return _nodes.Count();
        }

        public NativeList<int> GetNodeParents(int hash, Allocator allocator)
        {
            var result = new NativeList<int>(allocator);
            for (var i = 0; i < _nodeToParents.Length; i++)
            {
                var (childHash, parentHash) = _nodeToParents[i];
                if (!childHash.Equals(hash)) continue;
                result.Add(parentHash);
            }

            return result;
        }

        public NativeArray<Node> GetNodes(Allocator allocator)
        {
            return _nodes.GetValueArray(allocator);
        }

        public NativeList<Edge> GetEdges(Allocator allocator)
        {
            var result = new NativeList<Edge>(allocator);
            for (var i = 0; i < _nodeToParents.Length; i++)
            {
                var (childHash, parentHash) = _nodeToParents[i];
                result.Add(new Edge
                {
                    ChildHash = childHash,
                    ParentHash = parentHash
                });
            }
            return result;
        }

        public NativeArray<int> GetAllNodesHash(Allocator allocator)
        {
            return _nodes.GetKeyArray(allocator);
        }

        /// <summary>
        /// 读取指定node组的所有state
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="outStates"></param>
        /// <param name="allocator"></param>
        public void GetNodeStates(ref NativeList<Node> nodes,
            out NativeList<ValueTuple<int, State>> outStates, Allocator allocator)
        {
            outStates = new NativeList<ValueTuple<int, State>>(allocator);
            
            for (var i = 0; i < nodes.Length; i++)
            {
                var nodeHash = nodes[i].HashCode;
                for (var j = 0; j < _nodeStates.Length; j++)
                {
                    var (hash, _) = _nodeStates[j];
                    if (!hash.Equals(nodeHash)) continue;
                    outStates.Add(_nodeStates[j]);
                }
            }
        }

        /// <summary>
        /// 读取指定node的所有state到StateGroup中
        /// </summary>
        /// <param name="node"></param>
        /// <param name="allocator"></param>
        /// <param name="isPop">是否弹出（也就是取出后删除）</param>
        public StateGroup GetNodeStates(Node node, Allocator allocator, bool isPop = false)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _nodeStates.Length; i++)
            {
                var (hash, state) = _nodeStates[i];
                if (!hash.Equals(nodeHash)) continue;
                group.Add(state);
                if (!isPop) continue;
                _nodeStates.RemoveAt(i);    //不能用SwapBack，因为不按顺序的话，行为执行会变怪
                i--;
            }

            return group;
        }

        /// <summary>
        /// 将一组state全部加入到某一个node的states中
        /// </summary>
        /// <param name="states"></param>
        /// <param name="nodeHash"></param>
        /// <returns></returns>
        public void AddNodeStates(StateGroup states, int nodeHash)
        {
            for (var i = 0; i < states.Length(); i++)
            {
                var state = states[i];
                _nodeStates.Add((nodeHash, state));
            }
        }
        
        public State[] GetNodeStates(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _nodeStates.Length; i++)
            {
                var (hash, state) = _nodeStates[i];
                if (!hash.Equals(nodeHash)) continue;
                result.Add(state);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodePreconditions(Node node, Allocator allocator)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _preconditions.Length; i++)
            {
                var (hash, precondition) = _preconditions[i];
                if (!hash.Equals(nodeHash)) continue;
                group.Add(precondition);
            }

            return group;
        }
        
        public State[] GetNodePreconditions(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _preconditions.Length; i++)
            {
                var (hash, precondition) = _preconditions[i];
                if (!hash.Equals(nodeHash)) continue;
                result.Add(precondition);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodeEffects(Node node, Allocator allocator)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _effects.Length; i++)
            {
                var (hash, state) = _effects[i];
                if (!hash.Equals(nodeHash)) continue;
                group.Add(state);
            }

            return group;
        }
        
        public State[] GetNodeEffects(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _effects.Length; i++)
            {
                var (hash, effect) = _effects[i];
                if (!hash.Equals(nodeHash)) continue;
                result.Add(effect);
            }

            return result.ToArray();
        }

        public NativeList<int> GetChildren(int hash, Allocator allocator)
        {
            var result = new NativeList<int>(allocator);

            for (var i = 0; i < _nodeToParents.Length; i++)
            {
                var (childHash, parentHash) = _nodeToParents[i];
                if (!parentHash.Equals(hash)) continue;
                result.Add(childHash);
            }

            return result;
        }

        public Node GetStartNode()
        {
            return _nodes[StartNodeHash];
        }

        public Node GetGoalNode()
        {
            return _nodes[GoalNodeHash];
        }

        public void RemoveConnection(int childHash, int parentHash)
        {
            for (var i = 0; i < _nodeToParents.Length; i++)
            {
                var (cHash, pHash) = _nodeToParents[i];
                if (!cHash.Equals(childHash)) continue;
                if (!pHash.Equals(parentHash)) continue;
                _nodeToParents.RemoveAtSwapBack(i);
                return;
            }
        }

        public void RemoveNodeState(Node node, State state)
        {
            var nodeHash = node.HashCode;
            for (var i = 0; i < _nodeStates.Length; i++)
            {
                var (hash, aState) = _nodeStates[i];
                if (!hash.Equals(nodeHash)) continue;
                if (!aState.Equals(state)) continue;
                _nodeStates.RemoveAtSwapBack(i);
                break;
            }
        }

        /// <summary>
        /// 由于ActionExpand的并行会导致产生重复state
        /// 因此在CheckNodes中进行清理
        /// </summary>
        /// <param name="node"></param>
        public void CleanAllDuplicateStates(Node node)
        {
            CleanDuplicateStates(_preconditions, node);
            CleanDuplicateStates(_effects, node);
            CleanDuplicateStates(_nodeStates, node);
        }
        
        private void CleanDuplicateStates(NativeList<ValueTuple<int, State>> container, Node node)
        {
            var nodeHash = node.HashCode;
            for (var baseId = 0; baseId < container.Length; baseId++)
            {
                var (hash, state) = container[baseId];
                if (!hash.Equals(nodeHash)) continue;
                for (var otherId = baseId+1; otherId < container.Length; otherId++)
                {
                    var (otherHash, otherState) = container[otherId];
                    if (!hash.Equals(otherHash)) continue;
                    if (!state.Equals(otherState)) continue;
                    container.RemoveAtSwapBack(otherId);
                    otherId--;
                }
            }
        }

        public void AddDeadEndNode(int nodeHash)
        {
            _deadEndNodeHashes.Add(nodeHash);
        }


        public void Dispose()
        {
            _nodes.Dispose();

            _nodeToParents.Dispose();
            _nodeStates.Dispose();
            _preconditions.Dispose();
            _effects.Dispose();

            _states.Dispose();
            _effectHashes.Dispose();

            _deadEndNodeHashes.Dispose();
        }
    }
}