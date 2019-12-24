using Unity.Collections;
using Unity.Entities;

namespace DOTS.Struct
{
    public struct Edge
    {
        public Node Parent;
        public Node Child;
        public NativeString64 ActionName;

        public Edge(Node parent, Node child, NativeString64 actionName)
        {
            Parent = parent;
            Child = child;
            ActionName = actionName;
        }
    }
}