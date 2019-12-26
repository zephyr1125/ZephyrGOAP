using System;
using System.Globalization;
using DOTS.Struct;
using LitJson;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Logger
{
    [Serializable]
    public class GoapResult
    {
        public Entity Agent;
        
        public Node[] PathResult;

        public NodeView GoalNodeView;

        public DateTime TimeStart, TimeEnd;

        public void StartLog(Entity agent)
        {
            Agent = agent;
            TimeStart = DateTime.Now;
        }
        
        public void SetNodeGraph(ref NodeGraph nodeGraph)
        {
            GoalNodeView = new NodeView
            {
                Node = nodeGraph.GetGoalNode(),
                States = nodeGraph.GetNodeStates(nodeGraph.GetGoalNode())
            };
            ConstructNodeTree(ref nodeGraph, GoalNodeView);
        }

        private void ConstructNodeTree(ref NodeGraph nodeGraph, NodeView nodeView)
        {
            var children = nodeGraph.GetChildren(nodeView.Node);
            foreach (var child in children)
            {
                var newNode = new NodeView
                {
                    Node = child, States = nodeGraph.GetNodeStates(child),
                    Preconditions = nodeGraph.GetNodePreconditions(child),
                    Effects = nodeGraph.GetNodeEffects(child)
                };
                nodeView.AddChild(newNode);
                ConstructNodeTree(ref nodeGraph, newNode);
            }
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            PathResult = pathResult.ToArray();
            TimeEnd = DateTime.Now;
        }

        public void WriteJson(JsonWriter writer, EntityManager entityManager)
        {
            writer.WriteObjectStart();
            {
                writer.WritePropertyName("agent");
                writer.Write(entityManager.GetName(Agent));
                
                writer.WritePropertyName("time_start");
                writer.Write(TimeStart.ToString(CultureInfo.InvariantCulture));
                
                writer.WritePropertyName("time_end");
                writer.Write(TimeEnd.ToString(CultureInfo.InvariantCulture));
                
                writer.WritePropertyName("graph");
                GoalNodeView.WriteJson(writer, entityManager);
                
                writer.WritePropertyName("path");
                writer.WriteArrayStart();
                for (var i = 0; i < PathResult.Length; i++)
                {
                    writer.WriteObjectStart();
                    PathResult[i].WriteJson(writer, entityManager);
                    writer.WriteObjectEnd();
                }
                writer.WriteArrayEnd();
            }
            writer.WriteObjectEnd();
        }
    }
}