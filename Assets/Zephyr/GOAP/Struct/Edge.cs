using Unity.Collections;

namespace Zephyr.GOAP.Struct
{
    public struct Edge
    {
        public int ParentHash;
        public int ChildHash;

        public Edge(Node parent, Node child)
        {
            ParentHash = parent.HashCode;
            ChildHash = child.HashCode;
        }
    }
}