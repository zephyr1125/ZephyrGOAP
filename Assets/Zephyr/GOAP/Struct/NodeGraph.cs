using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Assertions;

namespace Zephyr.GOAP.Struct
{
    public struct NodeGraph : IDisposable
    {
        [ReadOnly]
        private NativeHashMap<int, Node> _nodes;
        
        [ReadOnly]
        private NativeMultiHashMap<int, Edge> _nodeToParent;
        
        [ReadOnly]
        private NativeList<int> _nodeStateIndices;
        [ReadOnly]
        private NativeList<State> _nodeStates;
        
        [ReadOnly]
        private NativeList<int> _preconditionIndices;
        [ReadOnly]
        private NativeList<State> _preconditions;
        
        [ReadOnly]
        private NativeList<int> _effectIndices;
        [ReadOnly]
        private NativeList<State> _effects;

        private int _goalNodeHash;

        /// <summary>
        /// 起点Node代表当前状态，没有Action
        /// </summary>
        private int _startNodeHash;
 
        public NodeGraph(int initialCapacity, Allocator allocator)
        {
            _nodes = new NativeHashMap<int, Node>(initialCapacity, allocator);
            _nodeToParent = new NativeMultiHashMap<int, Edge>(initialCapacity, allocator);
            
            _nodeStateIndices = new NativeList<int>(initialCapacity*4, allocator);
            _nodeStates = new NativeList<State>(initialCapacity*4, allocator);
            
            _preconditionIndices = new NativeList<int>(initialCapacity*4, allocator);
            _preconditions = new NativeList<State>(initialCapacity*4, allocator);
            
            _effectIndices = new NativeList<int>(initialCapacity*4, allocator);
            _effects = new NativeList<State>(initialCapacity*4, allocator);
            
            var startNode = new Node(){Name = new NativeString64("start")};
            _startNodeHash = startNode.HashCode;
            _nodes.Add(_startNodeHash, startNode);
            _goalNodeHash = 0;
        }

        public void SetGoalNode(Node goal, ref StateGroup stateGroup)
        {
            if (_goalNodeHash != 0)
            {
                _nodes.Remove(_goalNodeHash);
                for (var i = _nodeStateIndices.Length - 1; i >= 0; i--)
                {
                    if (!_nodeStateIndices[i].Equals(_goalNodeHash)) continue;
                    _nodeStateIndices.RemoveAtSwapBack(i);
                    _nodeStates.RemoveAtSwapBack(i);
                }
            }
            
            _goalNodeHash = goal.HashCode;
            _nodes.Add(_goalNodeHash, goal);
            foreach (var state in stateGroup)
            {
                _nodeStateIndices.Add(_goalNodeHash);
                _nodeStates.Add(state);
            }
        }

        public NativeHashMap<int, Node>.ParallelWriter NodesWriter => _nodes.AsParallelWriter();
        public NativeMultiHashMap<int, Edge>.ParallelWriter NodeToParentWriter => _nodeToParent.AsParallelWriter();
        
        public NativeList<int>.ParallelWriter NodeStateIndicesWriter => _nodeStateIndices.AsParallelWriter();
        public NativeList<State>.ParallelWriter NodeStatesWriter => _nodeStates.AsParallelWriter();
        
        public NativeList<int>.ParallelWriter PreconditionIndicesWriter => _preconditionIndices.AsParallelWriter();
        public NativeList<State>.ParallelWriter PreconditionsWriter => _preconditions.AsParallelWriter();
        
        public NativeList<int>.ParallelWriter EffectIndicesWriter => _effectIndices.AsParallelWriter();
        public NativeList<State>.ParallelWriter EffectsWriter => _effects.AsParallelWriter();

        /// <summary>
        /// 追加对起点的链接
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        public void LinkStartNode(Node parent)
        {
            //start node的iteration设置为此Node+1
            var iteration = parent.Iteration;
            var startNode = _nodes[_startNodeHash];
            if (startNode.Iteration <= iteration)
            {
                startNode.Iteration = iteration + 1;
                _nodes[_startNodeHash] = startNode;
            }
            _nodeToParent.Add(_startNodeHash, new Edge(parent, startNode));
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

        public NativeMultiHashMap<int, Edge>.Enumerator GetEdgeToParents(Node node)
        {
            return _nodeToParent.GetValuesForKey(node.HashCode);
        }

        public NativeArray<Node> GetNodes(Allocator allocator)
        {
            return _nodes.GetValueArray(allocator);
        }

        public NativeArray<Edge> GetEdges(Allocator allocator)
        {
            return _nodeToParent.GetValueArray(allocator);
        }

        public NativeArray<int> GetAllNodesHash(Allocator allocator)
        {
            return _nodes.GetKeyArray(allocator);
        }

        /// <summary>
        /// 读取指定node组的所有state
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="outIndices"></param>
        /// <param name="outStates"></param>
        /// <param name="allocator"></param>
        public void GetNodeStates(ref NativeList<Node> nodes, out NativeList<int> outIndices,
            out NativeList<State> outStates, Allocator allocator)
        {
            outIndices = new NativeList<int>(allocator);
            outStates = new NativeList<State>(allocator);
            
            for (var i = 0; i < nodes.Length; i++)
            {
                var nodeHash = nodes[i].HashCode;
                for (var j = 0; j < _nodeStateIndices.Length; j++)
                {
                    if (!_nodeStateIndices[j].Equals(nodeHash)) continue;
                    outIndices.Add(_nodeStateIndices[j]);
                    outStates.Add(_nodeStates[j]);
                }
            }
        }

        /// <summary>
        /// 读取指定node的所有state到StateGroup中
        /// </summary>
        /// <param name="node"></param>
        /// <param name="allocator"></param>
        public StateGroup GetNodeStates(Node node, Allocator allocator)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _nodeStateIndices.Length; i++)
            {
                if (!_nodeStateIndices[i].Equals(nodeHash)) continue;
                group.Add(_nodeStates[i]);
            }

            return group;
        }
        
        public State[] GetNodeStates(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _nodeStateIndices.Length; i++)
            {
                if (!_nodeStateIndices[i].Equals(nodeHash)) continue;
                result.Add(_nodeStates[i]);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodePreconditions(Node node, Allocator allocator)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _preconditionIndices.Length; i++)
            {
                if (!_preconditionIndices[i].Equals(nodeHash)) continue;
                group.Add(_preconditions[i]);
            }

            return group;
        }
        
        public State[] GetNodePreconditions(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _preconditionIndices.Length; i++)
            {
                if (!_preconditionIndices[i].Equals(nodeHash)) continue;
                result.Add(_preconditions[i]);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodeEffects(Node node, Allocator allocator)
        {
            var group = new StateGroup(1, allocator);
            var nodeHash = node.HashCode;
            for (var i = 0; i < _effectIndices.Length; i++)
            {
                if (!_effectIndices[i].Equals(nodeHash)) continue;
                group.Add(_effects[i]);
            }

            return group;
        }
        
        public State[] GetNodeEffects(Node node)
        {
            var result = new List<State>();
            var nodeHash = node.HashCode;
            for (var i = 0; i < _effectIndices.Length; i++)
            {
                if (!_effectIndices[i].Equals(nodeHash)) continue;
                result.Add(_effects[i]);
            }

            return result.ToArray();
        }

        public NativeList<int> GetChildren(Node node, Allocator allocator)
        {
            var result = new NativeList<int>(allocator);
            var allEdges = _nodeToParent.GetValueArray(Allocator.Temp);
            
            for (var i = 0; i < allEdges.Length; i++)
            {
                var edge = allEdges[i];
                if (node.HashCode.Equals(edge.ParentHash))
                {
                    result.Add(edge.ChildHash);
                }
            }

            allEdges.Dispose();
            return result;
        }

        public Node GetStartNode()
        {
            return _nodes[_startNodeHash];
        }

        public Node GetGoalNode()
        {
            return _nodes[_goalNodeHash];
        }

        public void RemoveEdge(Node child, Node parent)
        {
            var found = _nodeToParent.TryGetFirstValue(child.HashCode,
                out var edge, out var it);
            while (found)
            {
                if (edge.ParentHash.Equals(parent.HashCode))
                {
                    _nodeToParent.Remove(it);
                    return;
                }

                found = _nodeToParent.TryGetNextValue(out edge, ref it);
            }
        }

        public void ReplaceNodeState(Node node, State before, State after)
        {
            var nodeHash = node.HashCode;
            for (var i = 0; i < _nodeStateIndices.Length; i++)
            {
                if (!_nodeStateIndices[i].Equals(nodeHash)) continue;
                if (!_nodeStates[i].Equals(before)) continue;
                _nodeStates[i] = after;
                break;
            }
        }

        public void RemoveNodeState(Node node, State state)
        {
            var nodeHash = node.HashCode;
            for (var i = 0; i < _nodeStateIndices.Length; i++)
            {
                if (!_nodeStateIndices[i].Equals(nodeHash)) continue;
                if (!_nodeStates[i].Equals(state)) continue;
                _nodeStateIndices.RemoveAtSwapBack(i);
                _nodeStates.RemoveAtSwapBack(i);
                break;
            }
        }
        
        public void RemovePrecondition(Node node, State state)
        {
            var nodeHash = node.HashCode;
            for (var i = 0; i < _preconditionIndices.Length; i++)
            {
                if (!_preconditionIndices[i].Equals(nodeHash)) continue;
                if (!_preconditions[i].Equals(state)) continue;
                _preconditionIndices.RemoveAtSwapBack(i);
                _preconditions.RemoveAtSwapBack(i);
                break;
            }
        }
        
        public void ReplaceNodePrecondition(Node node, State before, State after)
        {
            var nodeHash = node.HashCode;
            for (var i = 0; i < _preconditionIndices.Length; i++)
            {
                if (!_preconditionIndices[i].Equals(nodeHash)) continue;
                if (!_preconditions[i].Equals(before)) continue;
                _preconditions[i] = after;
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
            CleanDuplicateStates(_preconditionIndices, _preconditions, node);
            CleanDuplicateStates(_effectIndices, _effects, node);
            CleanDuplicateStates(_nodeStateIndices, _nodeStates, node);
        }

        private void CleanDuplicateStates(NativeList<int> containerIndices,
            NativeList<State> container, Node node)
        {
            var nodeHash = node.HashCode;
            for (var i = 0; i < containerIndices.Length; i++)
            {
                if (!containerIndices[i].Equals(nodeHash)) continue;
                for (var j = i+1; j < containerIndices.Length; j++)
                {
                    if (!containerIndices[i].Equals(containerIndices[j])) continue;
                    if (!container[i].Equals(container[j])) continue;
                    containerIndices.RemoveAtSwapBack(j);
                    container.RemoveAtSwapBack(j);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>parents</returns>
        public void GetEdges(Node node, ref NativeQueue<Edge> queue)
        {
            var size = _nodeToParent.Count();
            var sizeOfNodeParents = _nodeToParent.CountValuesForKey(node.HashCode);
            var found = _nodeToParent.TryGetFirstValue(node.HashCode,
                out var foundEdge, out var it);
            while (found)
            {
                queue.Enqueue(foundEdge);
                found = _nodeToParent.TryGetNextValue(out foundEdge, ref it);
            }
        }


        public void Dispose()
        {
            _nodes.Dispose();
            _nodeToParent.Dispose();
            
            _nodeStateIndices.Dispose();
            _nodeStates.Dispose();
            
            _preconditionIndices.Dispose();
            _preconditions.Dispose();
            
            _effectIndices.Dispose();
            _effects.Dispose();
        }
    }
}