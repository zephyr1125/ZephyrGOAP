using System;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class EdgeLog
    {
        public int parentHash, childHash;

        public EdgeLog(Edge edge)
        {
            parentHash = edge.Parent.HashCode;
            childHash = edge.Child.HashCode;
        }
    }
}