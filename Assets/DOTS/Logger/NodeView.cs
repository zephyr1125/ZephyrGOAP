using System.Collections.Generic;
using System.Linq;
using DOTS.Struct;
using LitJson;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace DOTS.Logger
{
    public class NodeView
    {
        public string Name;

        public int Iteration;

        public float Reward;

        public EntityView NavigationSubject;

        public StateView[] States, Preconditions, Effects;

        public List<NodeView> Children;

        public bool IsPath;
        
        private int _hashCode;

        public static NodeView ConstructNodeTree(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            var goalNode = nodeGraph.GetGoalNode();
            var goalNodeView = new NodeView(ref nodeGraph, entityManager, goalNode);
            ConstructNodeTree(ref nodeGraph, entityManager, goalNodeView, goalNode);
            return goalNodeView;
        }
        
        private static void ConstructNodeTree(ref NodeGraph nodeGraph, EntityManager entityManager, NodeView nodeView, Node node)
        {
            var children = nodeGraph.GetChildren(node);
            foreach (var child in children)
            {
                var newNodeView = new NodeView(ref nodeGraph, entityManager, child);
                nodeView.AddChild(newNodeView);
                ConstructNodeTree(ref nodeGraph, entityManager, newNodeView, child);
            }
        }

        public NodeView(ref NodeGraph nodeGraph, EntityManager entityManager, Node node)
        {
            Name = node.Name.ToString();
            Iteration = node.Iteration;
            Reward = node.Reward;
            NavigationSubject = new EntityView(entityManager, node.NavigatingSubject);
            States = StateView.CreateStateViews(entityManager, nodeGraph.GetNodeStates(node));
            Preconditions = StateView.CreateStateViews(entityManager, nodeGraph.GetNodePreconditions(node));
            Effects = StateView.CreateStateViews(entityManager, nodeGraph.GetNodeEffects(node));
            _hashCode = node.HashCode;
        }
        
        public NodeView(JsonData data)
        {
            // Name = (string) data["name"];
            // Iteration = (int) data["iteration"];
            // Reward = (float) data["reward"];
            //
            // States = (from JsonData stateData in data["states"]
            //     select new State(stateData)).ToArray();
            // Preconditions = (from JsonData stateData in data["preconditions"]
            //     select new State(stateData)).ToArray();
            // Effects = (from JsonData stateData in data["effects"]
            //     select new State(stateData)).ToArray();
            //
            //
            // Children = new List<NodeView>();
            // foreach (JsonData childData in data["children"])
            // {
            //     Children.Add(new NodeView(childData));
            // }
        }
        
        public void AddChild(NodeView node)
        {
            if(Children == null) Children = new List<NodeView>();
            Children.Add(node);
        }

        public void WriteJson(JsonWriter writer, EntityManager entityManager)
        {
            // writer.WriteObjectStart();
            // {
            //     writer.WritePropertyName("name");
            //     writer.Write(Name);
            //
            //     writer.WritePropertyName("iteration");
            //     writer.Write(Iteration);
            //
            //     writer.WritePropertyName("reward");
            //     writer.Write(Reward);
            //
            //     writer.WritePropertyName("navigation_target");
            //     NavigationSubject.WriteJson(writer, entityManager);
            //
            //     WriteStatesJson(writer, entityManager, "states", States);
            //     WriteStatesJson(writer, entityManager, "preconditions", Preconditions);
            //     WriteStatesJson(writer, entityManager, "effects", Effects);
            //
            //     writer.WritePropertyName("children");
            //     writer.WriteArrayStart();
            //     if (Children != null)
            //     {
            //         foreach (var child in Children)
            //         {
            //             child.WriteJson(writer, entityManager);
            //         }
            //     }
            //     writer.WriteArrayEnd();
            // }
            // writer.WriteObjectEnd();
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

        /// <summary>
        /// 查看自己及children是否在path列表中，是的话则设置IsPath
        /// </summary>
        /// <param name="path"></param>
        public void SetPath(ref NativeList<Node> path)
        {
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i].HashCode != _hashCode) continue;
                IsPath = true;
                break;
            }

            if (Children == null) return;
            foreach (var child in Children)
            {
                child.SetPath(ref path);
            }
        }

        public void GetPath(ref List<NodeView> path)
        {
            if(IsPath)path.Add(this);
            
            if (Children == null) return;
            foreach (var child in Children)
            {
                child.GetPath(ref path);
            }
        }
    }
}