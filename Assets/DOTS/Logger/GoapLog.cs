using System;
using System.Text;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;
using LitJson;
using UnityEngine;

namespace DOTS.Logger
{
    [Serializable]
    public class GoapLog
    {
        private MultiDict<string, GoapResult> _results;
        
        private GoapResult _currentLog;
        
        public GoapLog()
        {
            _results = new MultiDict<string, GoapResult>();
        }

        public void StartLog(string agentName)
        {
            _currentLog = new GoapResult();
            _results.Add(agentName, _currentLog);
            _currentLog.StartLog(agentName);
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph)
        {
            _currentLog.SetNodeGraph(ref nodeGraph);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            _currentLog.SetPathResult(ref pathResult);
        }

        public NodeView GetGoalNodeView()
        {
            return _currentLog.GoalNodeView;
        }

        public Node[] GetPathResult()
        {
            return _currentLog.PathResult;
        }

        public string SaveToJson()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var sb = new StringBuilder();
            var writer = new JsonWriter(sb);
            
            /* [{agent="",plans={
                time="",
                graph={},
                path={}
                }
               },{...}]
             */
            
            writer.WriteArrayStart();
            {
                foreach (var key in _results.Keys)
                {
                    writer.WriteObjectStart();
                    {
                        writer.WritePropertyName("agent");
                        writer.Write(key);
                        writer.WritePropertyName("plans");
                        writer.WriteArrayStart();
                        foreach (var result in _results[key])
                        {
                            result.WriteJson(writer, entityManager);
                        }
                        writer.WriteArrayEnd();
                    }
                    writer.WriteObjectEnd();
                }
            }
            writer.WriteArrayEnd();
            return sb.ToString();
        }

        #region In Editor

        public GoapLog(JsonData data)
        {
            _results = new MultiDict<string, GoapResult>();

            foreach (JsonData results in data)
            {
                foreach (JsonData plan in results["plans"])
                {
                    var result = new GoapResult(plan);
                    _results.Add((string)plan["agent"], result);
                    //给默认第一个result为current
                    if (_currentLog == null) _currentLog = result;
                }
            }
        }

        public void DrawInfo()
        {
            GUI.color = Color.black;
            GUI.Label(new Rect(16, 16, 50, 16), _currentLog.AgentName);
            GUI.color = Color.white;
        }

        #endregion
    }
    
}