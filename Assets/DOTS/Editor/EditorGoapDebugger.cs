using DOTS.Debugger;
using DOTS.Logger;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Editor
{
    public class EditorGoapDebugger : IGoapDebugger
    {

        private GoapLog _goapLog;
        private global::System.Action<GoapLog> _onLogDone;

        public EditorGoapDebugger(global::System.Action<GoapLog> onLogDone)
        {
            _goapLog = new GoapLog();
            _onLogDone = onLogDone;
        }
        
        public void StartLog(EntityManager entityManager, Entity agent)
        {
            _goapLog.StartLog(entityManager, agent);
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            _goapLog.SetNodeGraph(ref nodeGraph, entityManager);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            _goapLog.SetPathResult(ref pathResult);
        }

        public void Log(string log)
        {
            Debug.Log(log);
        }

        public void LogDone()
        {
            _onLogDone?.Invoke(_goapLog);
        }
    }
}