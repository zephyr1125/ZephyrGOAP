using System;
using System.Collections.Generic;
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
        public string AgentName;

        public NodeView GoalNodeView;

        public DateTime TimeStart, TimeEnd;

        public GoapResult()
        {
            
        }
        
        public GoapResult(JsonData data)
        {
            AgentName = (string) data["agent"];
            TimeStart = DateTime.Parse((string) data["time_start"]);
            TimeEnd = DateTime.Parse((string) data["time_end"]);
            GoalNodeView = new NodeView(data["graph"]);
        }
        
        public void StartLog(string agentName)
        {
            AgentName = agentName;
            TimeStart = DateTime.Now;
        }
        
        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            GoalNodeView = NodeView.ConstructNodeTree(ref nodeGraph, entityManager);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            GoalNodeView.SetPath(ref pathResult);
            TimeEnd = DateTime.Now;
        }

        public NodeView[] GetPathResult()
        {
            var pathResult = new List<NodeView>();
            GoalNodeView.GetPath(ref pathResult);
            return pathResult.ToArray();
        }

        public void WriteJson(JsonWriter writer, EntityManager entityManager)
        {
            // writer.WriteObjectStart();
            // {
            //     writer.WritePropertyName("agent");
            //     writer.Write(AgentName);
            //     
            //     writer.WritePropertyName("time_start");
            //     writer.Write(TimeStart.ToString(CultureInfo.InvariantCulture));
            //     
            //     writer.WritePropertyName("time_end");
            //     writer.Write(TimeEnd.ToString(CultureInfo.InvariantCulture));
            //     
            //     writer.WritePropertyName("graph");
            //     GoalNodeView.WriteJson(writer, entityManager);
            // }
            // writer.WriteObjectEnd();
        }
    }
}