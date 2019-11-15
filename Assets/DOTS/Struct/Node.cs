using System;
using Action = DOTS.Component.Actions.Action;

namespace DOTS.Struct
{
    public struct Node : IEquatable<Node>
    {
        public Guid Guid;
        public Action Action;

        public Node(Action action) : this()
        {
            Action = action;
            Guid = Guid.NewGuid();
        }

        public bool Equals(Node other)
        {
            return Guid.Equals(other.Guid);
        }
    }
}