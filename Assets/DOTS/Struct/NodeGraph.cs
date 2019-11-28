using System;
using DOTS.Component;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Assertions;
using Action = DOTS.Component.Actions.Action;

namespace DOTS.Struct
{
    public struct NodeGraph : IDisposable
    {
        [NativeDisableParallelForRestriction]
        private NativeMultiHashMap<Node, Edge> _nodeToParent;
        [NativeDisableParallelForRestriction]
        public NativeMultiHashMap<Node, State> _nodeStates;

        private Node _goalNode;

        /// <summary>
        /// 起点Node代表当前状态，没有Action
        /// </summary>
        private Node _startNode;

        public NodeGraph(int initialCapacity, Allocator allocator)
        {
            _nodeToParent = new NativeMultiHashMap<Node, Edge>(initialCapacity, allocator);
            _nodeStates = new NativeMultiHashMap<Node, State>(initialCapacity, allocator);
            _goalNode = default;
            _startNode = new Node(){Name = new NativeString64("start")};
        }

        public NodeGraph Copy(Allocator allocator)
        {
            var newGraph = new NodeGraph(1, allocator);

            newGraph._goalNode = _goalNode;
            newGraph._startNode = _startNode;
            
            var nodeToParentKeys = _nodeToParent.GetKeyArray(Allocator.Temp);
            foreach (var key in nodeToParentKeys)
            {
                var values = _nodeToParent.GetValuesForKey(key);
                while (values.MoveNext())
                {
                    newGraph._nodeToParent.Add(key, values.Current);
                }
            }
            nodeToParentKeys.Dispose();
            
            var nodeStatesKeys = _nodeStates.GetKeyArray(Allocator.Temp);
            foreach (var key in nodeStatesKeys)
            {
                var values = _nodeStates.GetValuesForKey(key);
                while (values.MoveNext())
                {
                    newGraph._nodeStates.Add(key, values.Current);
                }
            }
            nodeStatesKeys.Dispose();

            return newGraph;
        }

        public void SetGoalNode(Node goal, ref StateGroup stateGroup)
        {
            _goalNode = goal;
            foreach (var state in stateGroup)
            {
                _nodeStates.Add(_goalNode, state);
            }
        }

        /// <summary>
        /// 追加对起点的链接
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        public void LinkStartNode(Node parent, NativeString64 actionName)
        {
            _nodeToParent.Add(_startNode, new Edge(parent, _startNode, actionName));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="stateGroup"></param>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        /// <returns>此node已存在</returns>
        public bool AddRouteNode(Node node, ref StateGroup stateGroup, Node parent,
            NativeString64 actionName)
        {
            node.Name = actionName;
            //第一个route必须连到goal
            if (Length() == 0)
            {
                Assert.AreEqual(_goalNode, parent,
                    "First route must connect to goal");
            }
            
            var existed = _nodeToParent.ContainsKey(node);
            _nodeToParent.Add(node, new Edge(parent, node, actionName));
            if(!existed){
                foreach (var state in stateGroup)
                {
                    _nodeStates.Add(node, state);
                }
            }
            return existed;
        }
        
        public bool AddRouteNode(Node node, ref State state, Node parent, NativeString64 actionName)
        {
            var stateGroup = new StateGroup(1, Allocator.Temp) {state};
            var existed = AddRouteNode(node, ref stateGroup, parent, actionName);
            stateGroup.Dispose();
            return existed;
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
            var nodes = new NativeList<Node>(keys.Length + 1, allocator) {_goalNode};
            nodes.AddRange(keys);
            keys.Dispose();
            return nodes;
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

        public Node GetStartNode()
        {
            return _startNode;
        }

        public Node GetGoalNode()
        {
            return _goalNode;
        }
        
        public void Dispose()
        {
            _nodeToParent.Dispose();
            _nodeStates.Dispose();
        }
    }
}