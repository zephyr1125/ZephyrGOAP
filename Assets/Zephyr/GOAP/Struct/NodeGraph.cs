using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace Zephyr.GOAP.Struct
{
    public struct NodeGraph : IDisposable
    {
        [ReadOnly]
        private NativeMultiHashMap<Node, Edge> _nodeToParent;
        [ReadOnly]
        private NativeMultiHashMap<Node, State> _nodeStates;
        [ReadOnly]
        private NativeMultiHashMap<Node, State> _preconditions;
        [ReadOnly]
        private NativeMultiHashMap<Node, State> _effects;

        private Node _goalNode;

        /// <summary>
        /// 起点Node代表当前状态，没有Action
        /// </summary>
        private Node _startNode;

        public NodeGraph(int initialCapacity, Allocator allocator)
        {
            _nodeToParent = new NativeMultiHashMap<Node, Edge>(initialCapacity, allocator);
            _nodeStates = new NativeMultiHashMap<Node, State>(initialCapacity*4, allocator);
            _preconditions = new NativeMultiHashMap<Node, State>(initialCapacity*3, allocator);
            _effects = new NativeMultiHashMap<Node, State>(initialCapacity*3, allocator);
            _goalNode = default;
            _startNode = new Node(){Name = new NativeString64("start")};
        }

        public void SetGoalNode(Node goal, ref StateGroup stateGroup)
        {
            _goalNode = goal;
            foreach (var state in stateGroup)
            {
                _nodeStates.Add(_goalNode, state);
            }
        }

        public NativeMultiHashMap<Node, Edge>.ParallelWriter NodeToParentWriter => _nodeToParent.AsParallelWriter();
        public NativeMultiHashMap<Node, State>.ParallelWriter NodeStateWriter => _nodeStates.AsParallelWriter();
        public NativeMultiHashMap<Node, State>.ParallelWriter PreconditionWriter => _preconditions.AsParallelWriter();
        public NativeMultiHashMap<Node, State>.ParallelWriter EffectWriter => _effects.AsParallelWriter();

        /// <summary>
        /// 追加对起点的链接
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        public void LinkStartNode(Node parent, NativeString64 actionName)
        {
            //start node的iteration设置为此Node+1
            var iteration = parent.Iteration;
            if (_startNode.Iteration <= iteration) _startNode.Iteration = iteration + 1;
            _nodeToParent.Add(_startNode, new Edge(parent, _startNode, actionName));
        }

        public Node this[int hashCode]
        {
            get
            {
                var resultNode = default(Node);
                var keys = _nodeStates.GetKeyArray(Allocator.Temp);
                foreach (var node in keys)
                {
                    if (node.GetHashCode() == hashCode)
                    {
                        resultNode = node;
                        break;
                    }
                }
                
                keys.Dispose();
                return resultNode;
            }
        }

        /// <summary>
        /// 询问node的数量
        /// </summary>
        /// <returns></returns>
        public int Length()
        {
            return _nodeToParent.Length + 1;
        }

        public NativeMultiHashMap<Node, Edge>.Enumerator GetEdgeToParents(Node node)
        {
            return _nodeToParent.GetValuesForKey(node);
        }

        public NativeList<Node> GetNodes(Allocator allocator)
        {
            var keys = _nodeToParent.GetKeyArray(Allocator.Temp);
            var keysDistinct = keys.Distinct();
            var nodes = new NativeList<Node>(keysDistinct.Count(), allocator) {_goalNode};
            foreach (var key in keysDistinct)
            {
                nodes.Add(key);
            }
            keys.Dispose();
            return nodes;
        }

        public NativeArray<Edge> GetEdges(Allocator allocator)
        {
            return _nodeToParent.GetValueArray(allocator);
        }

        /// <summary>
        /// 查询一组node是否已存在于图中
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<bool> NodesExisted(ref NativeList<Node> nodes, Allocator allocator)
        {
            var result = new NativeArray<bool>(nodes.Length, allocator);
            for (var i = 0; i < nodes.Length; i++)
            {
                result[i] = _nodeToParent.ContainsKey(nodes[i]);
            }

            return result;
        }

        public NativeArray<int> GetAllNodesHash(Allocator allocator)
        {
            var nodes = _nodeToParent.GetKeyArray(Allocator.Temp);
            var result = new NativeArray<int>(nodes.Length, allocator);
            for (var i = 0; i < nodes.Length; i++)
            {
                result[i] = nodes[i].HashCode;
            }
            nodes.Dispose();
            return result;
        }
        
        /// <summary>
        /// 读取指定node组的所有state
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="allocator"></param>
        public NativeMultiHashMap<Node, State> GetNodeStates(ref NativeList<Node> nodes, Allocator allocator)
        {
            var results = new NativeMultiHashMap<Node, State>(nodes.Length*6, allocator);
            for (var i = 0; i < nodes.Length; i++)
            {
                var states = _nodeStates.GetValuesForKey(nodes[i]);
                while (states.MoveNext())
                {
                    results.Add(nodes[i], states.Current);
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
            var states = _nodeStates.GetValuesForKey(node);
            return new StateGroup(1, states, allocator);
        }
        
        public State[] GetNodeStates(Node node)
        {
            var result = new List<State>();
            foreach (var state in _nodeStates.GetValuesForKey(node))
            {
                result.Add(state);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodePreconditions(Node node, Allocator allocator)
        {
            var states = _preconditions.GetValuesForKey(node);
            return new StateGroup(1, states, allocator);
        }
        
        public State[] GetNodePreconditions(Node node)
        {
            var result = new List<State>();
            foreach (var state in _preconditions.GetValuesForKey(node))
            {
                result.Add(state);
            }

            return result.ToArray();
        }
        
        public StateGroup GetNodeEffects(Node node, Allocator allocator)
        {
            var states = _effects.GetValuesForKey(node);
            return new StateGroup(1, states, allocator);
        }
        
        public State[] GetNodeEffects(Node node)
        {
            var result = new List<State>();
            foreach (var state in _effects.GetValuesForKey(node))
            {
                result.Add(state);
            }

            return result.ToArray();
        }

        public List<Node> GetChildren(Node node)
        {
            var result = new List<Node>();
            var values = _nodeToParent.GetValueArray(Allocator.Temp);
            foreach (var edge in values)
            {
                if (edge.Parent.Equals(node))
                {
                    result.Add(edge.Child);
                }
            }
            values.Dispose();
            return result;
        }

        public Node GetStartNode()
        {
            return _startNode;
        }

        public Node GetGoalNode()
        {
            return _goalNode;
        }

        public void RemoveEdge(Node child, Node parent)
        {
            var found = _nodeToParent.TryGetFirstValue(child, out var edge, out var it);
            while (found)
            {
                if (edge.Parent.Equals(parent))
                {
                    _nodeToParent.Remove(it);
                    return;
                }

                found = _nodeToParent.TryGetNextValue(out edge, ref it);
            }
        }

        public void ReplaceNodeState(Node node, State before, State after)
        {
            var found = _nodeStates.TryGetFirstValue(node, out var foundState, out var it);
            while (found)
            {
                if (foundState.Equals(before))
                {
                    _nodeStates.Remove(it);
                    _nodeStates.Add(node, after);
                    return;
                }
                found = _nodeStates.TryGetNextValue(out foundState, ref it);
            }
        }
        
        public void ReplaceNodePrecondition(Node node, State before, State after)
        {
            var found = _preconditions.TryGetFirstValue(node, out var foundState, out var it);
            while (found)
            {
                if (foundState.Equals(before))
                {
                    _preconditions.Remove(it);
                    _preconditions.Add(node, after);
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

        private void CleanDuplicateStates(NativeMultiHashMap<Node, State> container, Node node)
        {
            var lastState = new State();
            var found = container.TryGetFirstValue(node, out var foundState, out var it);
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


        public void Dispose()
        {
            _nodeToParent.Dispose();
            _nodeStates.Dispose();
            _preconditions.Dispose();
            _effects.Dispose();
        }
    }
}