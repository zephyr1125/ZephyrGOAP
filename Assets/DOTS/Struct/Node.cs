using System;
using Unity.Collections;
using Unity.Entities;
using Zephyr.DOTSAStar.Runtime.ComponentInterface;
using Action = DOTS.Component.Actions.Action;

namespace DOTS.Struct
{
    public struct Node : IEquatable<Node>, IPathFindingNode, IBufferElementData
    {
        public NativeString64 Name;
        
        /// <summary>
        /// 第一次展开到这个Node时的层数, goal=0
        /// </summary>
        public int Iteration;
        
        /// <summary>
        /// 用于比较两个Node是否是同一个node
        /// 即只要两个Node的states全部一致即为同一个node
        /// </summary>
        private readonly int _hashCode;

        /// <summary>
        /// 当node被当做path存在agent上时，用bitmask指示其preconditions和effects对应的states in buffer
        /// </summary>
        public ulong PreconditionsBitmask;
        public ulong EffectsBitmask;

        public Node(ref StateGroup states, string name, int iteration) : this()
        {
            Name = new NativeString64(name);
            Iteration = iteration;
            _hashCode = states.GetHashCode();
        }
        
        public Node(ref State state, string name, int iteration) : this()
        {
            Name = new NativeString64(name);
            Iteration = iteration;
            _hashCode = state.GetHashCode();
        }

        public bool Equals(Node other)
        {
            return _hashCode.Equals(other._hashCode);
        }

        public int GetCost([ReadOnly]ref NodeGraph nodeGraph)
        {
            //todo 需要计算cost
            return 1;
        }

        public float Heuristic([ReadOnly]ref NodeGraph nodeGraph)
        {
            //todo 目前直接使用与goal的距离
            return Iteration - nodeGraph.GetGoalNode().Iteration;
        }

        public void GetNeighbours([ReadOnly]ref NodeGraph nodeGraph, ref NativeList<int> neighboursId)
        {
            //所有的parent即为neighbour
            var edges = nodeGraph.GetEdgeToParents(this);
            foreach (var edge in edges)
            {
                neighboursId.Add(edge.Parent._hashCode);
            }
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}