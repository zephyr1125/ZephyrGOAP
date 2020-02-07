using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Debugger;
using Zephyr.GOAP.Logger;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Editor
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
        
        public void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager)
        {
            _goapLog.SetCurrentStates(ref currentStates, entityManager);
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