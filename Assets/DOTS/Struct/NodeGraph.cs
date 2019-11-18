using System;
using DOTS.Component;
using Unity.Collections;

namespace DOTS.Struct
{
    public struct NodeGraph : IDisposable
    {
        [NativeDisableParallelForRestriction]
        private NativeList<Node> _nodes;
        [NativeDisableParallelForRestriction]
        private NativeHashMap<Node, Node> _nodeToParent;
        [NativeDisableParallelForRestriction]
        private NativeMultiHashMap<Node, Node> _nodeToChildren;
        [NativeDisableParallelForRestriction]
        private NativeMultiHashMap<Node, State> _nodeStates;

        public NodeGraph(int initialCapacity, Allocator allocator)
        {
            _nodes = new NativeList<Node>(allocator);
            _nodeToParent = new NativeHashMap<Node, Node>(initialCapacity, allocator);
            _nodeToChildren = new NativeMultiHashMap<Node, Node>(initialCapacity, allocator);
            _nodeStates = new NativeMultiHashMap<Node, State>(initialCapacity, allocator);
        }
        
        public void Add(Node node, ref StateGroup stateGroup, Node parent)
        {
            _nodes.Add(node);
            foreach (var state in stateGroup)
            {
                _nodeStates.Add(node, state);
            }

            if (!parent.Equals(default))
            {
                _nodeToParent[node] = parent;
                _nodeToChildren.Add(parent, node);
            }
        }
        
        public void Add(Node node, State state, Node parent)
        {
            _nodes.Add(node);
            _nodeStates.Add(node, state);

            if (!parent.Equals(default))
            {
                _nodeToParent[node] = parent;
                _nodeToChildren.Add(parent, node);
            }
        }

        /// <summary>
        /// 询问node的数量
        /// </summary>
        /// <returns></returns>
        public int Length()
        {
            return _nodes.Length;
        }

        public Node GetNode(int id)
        {
            return _nodes[id];
        }

        public Node GetParent(Node node)
        {
            return _nodeToParent[node];
        }

        public NativeMultiHashMap<Node, Node>.Enumerator GetChildren(Node node)
        {
            return _nodeToChildren.GetValuesForKey(node);
        }

        /// <summary>
        /// 读取指定node的所有state到StateGroup中
        /// </summary>
        /// <param name="node"></param>
        /// <param name="allocator"></param>
        public StateGroup GetStateGroup(Node node, Allocator allocator)
        {
            var states = _nodeStates.GetValuesForKey(node);
            return new StateGroup(1, states, allocator);
        }
        
        public void Dispose()
        {
            _nodes.Dispose();
            _nodeToParent.Dispose();
            _nodeToChildren.Dispose();
            _nodeStates.Dispose();
        }
    }
}