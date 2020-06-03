using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component.Trait;

namespace Zephyr.GOAP.Struct
{
    public struct NodeGraph : IDisposable
    {
        [ReadOnly]
        private NativeHashMap<int, Node> _nodes;
        
        [ReadOnly]
        private NativeList<int> _nodeToParentIndices;
        [ReadOnly]
        private NativeList<int> _nodeToParents;
        
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

        public NativeList<int> _deadEndNodeHashes;

        private int _goalNodeHash;

        /// <summary>
        /// 起点Node代表当前状态，没有Action
        /// </summary>
        private int _startNodeHash;
 
        public NodeGraph(int initialCapacity, ref DynamicBuffer<State> startNodeStates, Allocator allocator)
        {
            _nodes = new NativeHashMap<int, Node>(initialCapacity, allocator);
            
            _nodeToParentIndices = new NativeList<int>(initialCapacity*4, allocator);
            _nodeToParents = new NativeList<int>(initialCapacity*4, allocator);
            
            _nodeStateIndices = new NativeList<int>(initialCapacity*4, allocator);
            _nodeStates = new NativeList<State>(initialCapacity*4, allocator);
            
            _preconditionIndices = new NativeList<int>(initialCapacity*4, allocator);
            _preconditions = new NativeList<State>(initialCapacity*4, allocator);
            
            _effectIndices = new NativeList<int>(initialCapacity*4, allocator);
            _effects = new NativeList<State>(initialCapacity*4, allocator);
            
            _deadEndNodeHashes = new NativeList<int>(allocator);
            
            var startNode = new Node(){Name = new NativeString64("start")};
            _startNodeHash = startNode.HashCode;
            _nodes.Add(_startNodeHash, startNode);
            for (var i = 0; i < startNodeStates.Length; i++)
            {
                _effectIndices.Add(_startNodeHash);
                _effects.Add(startNodeStates[i]);
            }
            
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
        
        public NativeList<int>.ParallelWriter NodeToParentIndicesWriter => _nodeToParentIndices.AsParallelWriter();
        public NativeList<int>.ParallelWriter NodeToParentsWriter => _nodeToParents.AsParallelWriter();
        
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
            _nodeToParentIndices.Add(_startNodeHash);
            _nodeToParents.Add(parent.HashCode);
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

        public NativeList<int> GetNodeParents(int childHash, Allocator allocator)
        {
            var result = new NativeList<int>(allocator);
            for (var i = 0; i < _nodeToParentIndices.Length; i++)
            {
                if (!_nodeToParentIndices[i].Equals(childHash)) continue;
                result.Add(_nodeToParents[i]);
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
            for (var i = 0; i < _nodeToParentIndices.Length; i++)
            {
                result.Add(new Edge
                {
                    ChildHash = _nodeToParentIndices[i],
                    ParentHash = _nodeToParents[i]
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

        public NativeList<int> GetChildren(int parentHash, Allocator allocator)
        {
            var result = new NativeList<int>(allocator);

            for (var i = 0; i < _nodeToParents.Length; i++)
            {
                if (!_nodeToParents[i].Equals(parentHash)) continue;
                result.Add(_nodeToParentIndices[i]);
            }

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

        public void RemoveConnection(int childHash, int parentHash)
        {
            for (var i = 0; i < _nodeToParentIndices.Length; i++)
            {
                if (!_nodeToParentIndices[i].Equals(childHash)) continue;
                if (!_nodeToParents[i].Equals(parentHash)) continue;
                _nodeToParentIndices.RemoveAtSwapBack(i);
                _nodeToParents.RemoveAtSwapBack(i);
                return;
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

        /// <summary>
        /// 检查特定iteration之前不应出现
        /// </summary>
        public void DebugCheckNoCookStateBeforeIteration4(int iteration)
        {
            if (iteration > 4) return;
            
            for (var effectId = 0; effectId < _effectIndices.Length; effectId++)
            {
                var nodeHash = _effectIndices[effectId];
                var nodeEffect = _effects[effectId];
                if (!nodeEffect.Trait.Equals(typeof(ItemSourceTrait))) continue;

                if (nodeEffect.ValueString.Equals("roast_apple"))
                {
                    Debug.LogError($"({nodeHash})\"roast_apple\" before iteration 4!");
                }else if (nodeEffect.ValueString.Equals("feast"))
                {
                    Debug.LogError($"({nodeHash})\"feast\" before iteration 4!");
                }
            }
        }

        /// <summary>
        /// 出现了CookAction的node之后，检查是否出现了同时2个effect的Cook，即为错误特征
        /// </summary>
        /// <param name="uncheckedNodes"></param>
        public void DebugCheckNodeEffects(ref NativeHashMap<int, Node> uncheckedNodes)
        {
            var nodeHashes = _nodes.GetKeyArray(Allocator.Temp);
            for (var nodeId = 0; nodeId < nodeHashes.Length; nodeId++)
            {
                var nodeHash = nodeHashes[nodeId];
                if (!uncheckedNodes.ContainsKey(nodeHash)) continue;
                if (!uncheckedNodes[nodeHash].Name.Equals("CookAction")) continue;
                
                var hasRoastApple = false;
                var hasFeast = false;
            
                for (var effectId = 0; effectId < _effectIndices.Length; effectId++)
                {
                    if (!_effectIndices[effectId].Equals(nodeHash)) continue;
                    var nodeEffect = _effects[effectId];

                    if (nodeEffect.ValueString.Equals("roast_apple"))
                    {
                        hasRoastApple = true;
                    }else if (nodeEffect.ValueString.Equals("feast"))
                    {
                        hasFeast = true;
                    }
                }

                if (hasRoastApple && hasFeast)
                {
                    Debug.LogError($"({nodeHash}) 2 Effects For Cook!");
                }
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
            for (var baseId = 0; baseId < containerIndices.Length; baseId++)
            {
                if (!containerIndices[baseId].Equals(nodeHash)) continue;
                for (var checkId = baseId+1; checkId < containerIndices.Length; checkId++)
                {
                    if (!containerIndices[baseId].Equals(containerIndices[checkId])) continue;
                    if (!container[baseId].Equals(container[checkId])) continue;
                    containerIndices.RemoveAtSwapBack(checkId);
                    container.RemoveAtSwapBack(checkId);
                    checkId--;
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

            _nodeToParentIndices.Dispose();
            _nodeToParents.Dispose();
            
            _nodeStateIndices.Dispose();
            _nodeStates.Dispose();
            
            _preconditionIndices.Dispose();
            _preconditions.Dispose();
            
            _effectIndices.Dispose();
            _effects.Dispose();

            _deadEndNodeHashes.Dispose();
        }
    }
}