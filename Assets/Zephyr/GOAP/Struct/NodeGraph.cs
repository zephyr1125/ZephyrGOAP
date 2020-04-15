using System;
using System.Collections.Generic;
using Unity.Collections;
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
        private NativeMultiHashMap<int, State> _nodeStates;
        [ReadOnly]
        private NativeMultiHashMap<int, State> _preconditions;
        [ReadOnly]
        private NativeMultiHashMap<int, State> _effects;

        private int _goalNodeHash;

        /// <summary>
        /// 起点Node代表当前状态，没有Action
        /// </summary>
        private int _startNodeHash;

        public NodeGraph(int initialCapacity, Allocator allocator)
        {
            _nodes = new NativeHashMap<int, Node>(initialCapacity, allocator);
            _nodeToParent = new NativeMultiHashMap<int, Edge>(initialCapacity, allocator);
            _nodeStates = new NativeMultiHashMap<int, State>(initialCapacity*4, allocator);
            _preconditions = new NativeMultiHashMap<int, State>(initialCapacity*3, allocator);
            _effects = new NativeMultiHashMap<int, State>(initialCapacity*3, allocator);
            
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
                _nodeStates.Remove(_goalNodeHash);
            }
            
            _goalNodeHash = goal.HashCode;
            _nodes.Add(_goalNodeHash, goal);
            foreach (var state in stateGroup)
            {
                _nodeStates.Add(_goalNodeHash, state);
            }
        }

        public NativeHashMap<int, Node>.ParallelWriter NodesWriter => _nodes.AsParallelWriter();
        public NativeMultiHashMap<int, Edge>.ParallelWriter NodeToParentWriter => _nodeToParent.AsParallelWriter();
        public NativeMultiHashMap<int, State>.ParallelWriter NodeStateWriter => _nodeStates.AsParallelWriter();
        public NativeMultiHashMap<int, State>.ParallelWriter PreconditionWriter => _preconditions.AsParallelWriter();
        public NativeMultiHashMap<int, State>.ParallelWriter EffectWriter => _effects.AsParallelWriter();

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
        /// <param name="allocator"></param>
        public NativeMultiHashMap<int, State> GetNodeStates(ref NativeList<Node> nodes, Allocator allocator)
        {
            var results = new NativeMultiHashMap<int, State>(nodes.Length*6, allocator);
            for (var i = 0; i < nodes.Length; i++)
            {
                var states = _nodeStates.GetValuesForKey(nodes[i].HashCode);
                while (states.MoveNext())
                {
                    results.Add(nodes[i].HashCode, states.Current);
                }
            }

            return results;
        }

        /// <summary>
        /// 读取指定node的所有state到StateGroup中
        /// </summary>
        /// <param name="node"></param>
        /// <param name="allocator"></param>
        public StateGroup GetNodeStates(Node node, Allocator allocator)
        {
            var states = _nodeStates.GetValuesForKey(node.HashCode);
            return new StateGroup(1, states, allocator);
        }
        
        public State[] GetNodeStates(Node node)
        {
            var result = new List<State>();
            foreach (var state in _nodeStates.GetValuesForKey(node.HashCode))
            {
                result.Add(state);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodePreconditions(Node node, Allocator allocator)
        {
            var states = _preconditions.GetValuesForKey(node.HashCode);
            return new StateGroup(1, states, allocator);
        }
        
        public State[] GetNodePreconditions(Node node)
        {
            var result = new List<State>();
            foreach (var state in _preconditions.GetValuesForKey(node.HashCode))
            {
                result.Add(state);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodeEffects(Node node, Allocator allocator)
        {
            var states = _effects.GetValuesForKey(node.HashCode);
            return new StateGroup(1, states, allocator);
        }
        
        public State[] GetNodeEffects(Node node)
        {
            var result = new List<State>();
            foreach (var state in _effects.GetValuesForKey(node.HashCode))
            {
                result.Add(state);
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
            var found = _nodeStates.TryGetFirstValue(node.HashCode,
                out var foundState, out var it);
            while (found)
            {
                if (foundState.Equals(before))
                {
                    _nodeStates.Remove(it);
                    _nodeStates.Add(node.HashCode, after);
                    return;
                }
                found = _nodeStates.TryGetNextValue(out foundState, ref it);
            }
        }
        
        public void ReplaceNodePrecondition(Node node, State before, State after)
        {
            var found = _preconditions.TryGetFirstValue(node.HashCode,
                out var foundState, out var it);
            while (found)
            {
                if (foundState.Equals(before))
                {
                    _preconditions.Remove(it);
                    _preconditions.Add(node.HashCode, after);
                    return;
                }
                found = _preconditions.TryGetNextValue(out foundState, ref it);
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

        private void CleanDuplicateStates(NativeMultiHashMap<int, State> container, Node node)
        {
            var lastState = new State();
            var found = container.TryGetFirstValue(node.HashCode,
                out var foundState, out var it);
            while (found)
            {
                if (foundState.Equals(lastState))
                {
                    container.Remove(it);
                }

                lastState = foundState;
                found = container.TryGetNextValue(out foundState, ref it);
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
            _nodeStates.Dispose();
            _preconditions.Dispose();
            _effects.Dispose();
        }
    }
}