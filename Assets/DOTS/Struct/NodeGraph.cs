using System;
using DOTS.Component;
using Unity.Collections;

namespace DOTS.Struct
{
    public struct NodeGraph : IDisposable
    {
        [NativeDisableParallelForRestriction]
        public NativeList<Node> Nodes;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<Node, Node> NodeToParent;
        [NativeDisableParallelForRestriction]
        public NativeMultiHashMap<Node, Node> NodeToChildren;
        [NativeDisableParallelForRestriction]
        public NativeMultiHashMap<Node, State> NodeStates;

        public void Dispose()
        {
            Nodes.Dispose();
            NodeToParent.Dispose();
            NodeToChildren.Dispose();
            NodeStates.Dispose();
        }
    }
}