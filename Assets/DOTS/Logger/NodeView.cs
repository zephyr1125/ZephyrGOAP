using System.Collections.Generic;
using DOTS.Struct;
using LitJson;
using Unity.Entities;

namespace DOTS.Logger
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

        public void WriteJson(JsonWriter writer, EntityManager entityManager)
        {
            writer.WriteObjectStart();
            {
                Node.WriteJson(writer, entityManager);
            
                WriteStatesJson(writer, entityManager, "states", States);
                WriteStatesJson(writer, entityManager, "preconditions", Preconditions);
                WriteStatesJson(writer, entityManager, "effects", Effects);

                writer.WritePropertyName("children");
                writer.WriteArrayStart();
                if (Children != null)
                {
                    foreach (var child in Children)
                    {
                        child.WriteJson(writer, entityManager);
                    }
                }
                writer.WriteArrayEnd();
            }
            writer.WriteObjectEnd();
        }

        private void WriteStatesJson(JsonWriter writer, EntityManager entityManager,
            string name, State[] states)
        {
            writer.WritePropertyName(name);
            writer.WriteArrayStart();
            if (states != null)
            {
                foreach (var state in states)
                {
                    state.WriteJson(writer, entityManager);
                }
            }
            writer.WriteArrayEnd();
        }
    }
}