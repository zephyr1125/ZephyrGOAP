using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
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
        private NativeList<ValueTuple<int,int>> _effectHashes;
        
        [ReadOnly]
        private NativeList<ValueTuple<int,int>> _preconditionHashes;
        
        [ReadOnly]
        private NativeList<ValueTuple<int,int>> _requireHashes;


        public NativeList<int> _deadEndNodeHashes;

        /// <summary>
        /// 起点Node代表当前状态，没有Action
        /// </summary>
        public int StartNodeHash { get; }
        
        public int GoalNodeHash { get; private set; }

        public NodeGraph(int initialCapacity, ref DynamicBuffer<State> startRequires, Allocator allocator) : this()
        {
            _nodes = new NativeHashMap<int, Node>(initialCapacity, allocator);
            
            _nodeToParents = new NativeList<ValueTuple<int, int>>(initialCapacity*4, allocator);
            
            _states = new NativeHashMap<int, State>(initialCapacity*4, allocator);
            _effectHashes = new NativeList<ValueTuple<int,int>>(initialCapacity*2, allocator);
            _preconditionHashes = new NativeList<ValueTuple<int,int>>(initialCapacity*2, allocator);
            _requireHashes = new NativeList<ValueTuple<int,int>>(initialCapacity*4, allocator);
            
            _deadEndNodeHashes = new NativeList<int>(allocator);
            
            var startNode = new Node {Name = "start"};
            StartNodeHash = startNode.HashCode;
            _nodes.Add(StartNodeHash, startNode);
            for (var i = 0; i < startRequires.Length; i++)
            {
               AddEffect(startRequires[i], StartNodeHash);
            }
            
            GoalNodeHash = 0;
        }

        public void AddEffect(State effect, int nodeHash)
        {
            var effectHash = effect.GetHashCode();
            _effectHashes.Add((nodeHash, effectHash));
            if (_states.ContainsKey(effectHash)) return;
            _states.Add(effectHash, effect);
        }

        public void SetGoalNode(Node goal, ref StateGroup stateGroup)
        {
            //先清除旧的
            if (GoalNodeHash != 0)
            {
                _nodes.Remove(GoalNodeHash);
                for (var i = _requireHashes.Length - 1; i >= 0; i--)
                {
                    var (nodeHash, stateHash) = _requireHashes[i];
                    if (!nodeHash.Equals(GoalNodeHash)) continue;
                    // _states.Remove(stateHash); //不可以从states里清除，因为可能有多个node引用到
                    _requireHashes.RemoveAtSwapBack(i);
                }
            }
            
            GoalNodeHash = goal.HashCode;
            _nodes.Add(GoalNodeHash, goal);
            foreach (var state in stateGroup)
            {
                var stateHash = state.GetHashCode();
                _requireHashes.Add((GoalNodeHash, stateHash));
                _states.TryAdd(stateHash, state);
            }
        }

        public NativeHashMap<int, Node>.ParallelWriter NodesWriter => _nodes.AsParallelWriter();
        
        public NativeList<ValueTuple<int, int>>.ParallelWriter NodeToParentsWriter => _nodeToParents.AsParallelWriter();
        
        public NativeHashMap<int, State>.ParallelWriter StatesWriter => _states.AsParallelWriter();

        public NativeList<ValueTuple<int,int>>.ParallelWriter EffectHashesWriter =>
            _effectHashes.AsParallelWriter();
        
        public NativeList<ValueTuple<int,int>>.ParallelWriter PreconditionHashesWriter =>
            _preconditionHashes.AsParallelWriter();
        
        public NativeList<ValueTuple<int, int>>.ParallelWriter RequireHashesWriter =>
            _requireHashes.AsParallelWriter();

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
        /// 读取指定node组的所有require
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="outStates"></param>
        /// <param name="allocator"></param>
        public void GetRequires(ref NativeList<Node> nodes,
            out NativeList<ValueTuple<int, State>> outStates, Allocator allocator)
        {
            outStates = new NativeList<ValueTuple<int, State>>(allocator);
            
            for (var i = 0; i < nodes.Length; i++)
            {
                var nodeHash = nodes[i].HashCode;
                for (var stateHashId = 0; stateHashId < _requireHashes.Length; stateHashId++)
                {
                    var (aNodeHash, stateHash) = _requireHashes[stateHashId];
                    if (!aNodeHash.Equals(nodeHash)) continue;
                    outStates.Add((nodeHash, _states[stateHash]));
                }
            }
        }

        /// <summary>
        /// 读取指定node的所有require到StateGroup中
        /// </summary>
        /// <param name="node"></param>
        /// <param name="allocator"></param>
        /// <param name="isPop">是否弹出（也就是取出后删除）</param>
        public StateGroup GetRequires(Node node, Allocator allocator, bool isPop = false)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _requireHashes.Length; i++)
            {
                var (aNodeHash, stateHash) = _requireHashes[i];
                if (!aNodeHash.Equals(nodeHash)) continue;
                group.Add(_states[stateHash]);
                if (!isPop) continue;
                _requireHashes.RemoveAt(i);    //不能用SwapBack，因为不按顺序的话，行为执行会变怪
                i--;
            }

            return group;
        }

        /// <summary>
        /// 将一组state全部加入到某一个node的require中
        /// </summary>
        /// <param name="states"></param>
        /// <param name="nodeHash"></param>
        /// <returns></returns>
        public void AddRequires(StateGroup states, int nodeHash)
        {
            for (var i = 0; i < states.Length(); i++)
            {
                var state = states[i];
                var stateHash = state.GetHashCode();
                _states.TryAdd(stateHash, state);
                _requireHashes.Add((nodeHash, stateHash));
            }
        }
        
        public State[] GetRequires(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _requireHashes.Length; i++)
            {
                var (aNodeHash, stateHash) = _requireHashes[i];
                if (!aNodeHash.Equals(nodeHash)) continue;
                result.Add(_states[stateHash]);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodePreconditions(Node node, Allocator allocator)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _preconditionHashes.Length; i++)
            {
                var (aNodeHash, preconditionHash) = _preconditionHashes[i];
                if (!aNodeHash.Equals(nodeHash)) continue;
                group.Add(_states[preconditionHash]);
            }

            return group;
        }
        
        public State[] GetNodePreconditions(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _preconditionHashes.Length; i++)
            {
                var (aNodeHash, preconditionHash) = _preconditionHashes[i];
                if (!aNodeHash.Equals(nodeHash)) continue;
                result.Add(_states[preconditionHash]);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodeEffects(Node node, Allocator allocator)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _effectHashes.Length; i++)
            {
                var (aNodeHash, effectHash) = _effectHashes[i];
                if (!nodeHash.Equals(aNodeHash)) continue;
                group.Add(_states[effectHash]);
            }

            return group;
        }
        
        public State[] GetNodeEffects(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _effectHashes.Length; i++)
            {
                var (aNodeHash, effectHash) = _effectHashes[i];
                if (!nodeHash.Equals(aNodeHash)) continue;
                result.Add(_states[effectHash]);
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

        /// <summary>
        /// 由于ActionExpand的并行会导致产生重复state
        /// 因此在CheckNodes中进行清理
        /// </summary>
        /// <param name="node"></param>
        public void CleanAllDuplicateStates(Node node)
        {
            CleanDuplicateStates(_preconditionHashes, node);
            CleanDuplicateStates(_effectHashes, node);
            CleanDuplicateStates(_requireHashes, node);
        }
        
        private void CleanDuplicateStates<T>(NativeList<ValueTuple<int, T>> container, Node node)
        {
            var nodeHash = node.HashCode;
            for (var baseId = 0; baseId < container.Length; baseId++)
            {
                var (key, value) = container[baseId];
                if (!key.Equals(nodeHash)) continue;
                for (var otherId = baseId+1; otherId < container.Length; otherId++)
                {
                    var (otherKey, otherValue) = container[otherId];
                    if (!key.Equals(otherKey)) continue;
                    if (!value.Equals(otherValue)) continue;
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

            _states.Dispose();
            _effectHashes.Dispose();
            _preconditionHashes.Dispose();
            _requireHashes.Dispose();

            _deadEndNodeHashes.Dispose();
        }
    }
}