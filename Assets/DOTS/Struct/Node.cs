using System;
using Unity.Entities;
using Action = DOTS.Component.Actions.Action;

namespace DOTS.Struct
{
    public struct Node : IEquatable<Node>
    {
        public NativeString64 Name;
        
        /// <summary>
        /// 用于比较两个Node是否是同一个node
        /// 即只要两个Node的states全部一致即为同一个node
        /// </summary>
        private readonly int _hashCode;

        public Node(ref StateGroup states, string name) : this()
        {
            Name = new NativeString64(name);
            _hashCode = states.GetHashCode();
        }
        
        public Node(ref State state, string name) : this()
        {
            Name = new NativeString64(name);
            _hashCode = state.GetHashCode();
        }

        public bool Equals(Node other)
        {
            return _hashCode.Equals(other._hashCode);
        }
    }
}