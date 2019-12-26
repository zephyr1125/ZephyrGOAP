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
        private MultiDict<Entity, GoapResult> _results;
        
        private GoapResult _currentLog;
        
        public GoapLog()
        {
            _results = new MultiDict<Entity, GoapResult>();
        }
        
        public void StartLog(Entity agent)
        {
            _currentLog = new GoapResult();
            _results.Add(agent, _currentLog);
            _currentLog.StartLog(agent);
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
                        writer.Write(entityManager.GetName(key));
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
    }
    
}