using System.Collections.Generic;
using DOTS.Struct;

namespace DOTS.Test.Debugger
{
    public class NodeView
    {
        public Node Node;

        public State[] States, Preconditions, Effects;

        public List<NodeView> Children;

        public void AddChild(NodeView node)
        {
            if(Children == null) Children = new List<NodeView>();
            Children.Add(node);
        }
    }
}