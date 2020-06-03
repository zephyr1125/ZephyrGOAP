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
        
        public void StartLog(EntityManager entityManager)
        {
            _goapLog.StartLog(entityManager);
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            _goapLog.SetNodeGraph(ref nodeGraph, entityManager);
        }

        public void SetPathResult(EntityManager entityManager,
            ref NativeArray<Entity> pathEntities, ref NativeList<Node> pathResult)
        {
            _goapLog.SetPathResult(entityManager, ref pathEntities, ref pathResult);
        }

        public void SetNodeAgentInfos(EntityManager entityManager,
            ref NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos)
        {
            _goapLog.SetNodeAgentInfos(entityManager, ref nodeAgentInfos);
        }

        public void SetNodeTotalTimes(ref NativeHashMap<int, float> nodeTimes)
        {
            _goapLog.SetNodeTotalTimes(ref nodeTimes);
        }

        public void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager)
        {
            _goapLog.SetCurrentStates(ref currentStates, entityManager);
        }

        public void SetSpecifiedPreconditions(EntityManager entityManager,
            ref NativeList<int> pathNodeSpecifiedPreconditionIndices,
            ref NativeList<State> pathNodeSpecifiedPreconditions)
        {
            _goapLog.SetSpecifiedPreconditions(entityManager,
                ref pathNodeSpecifiedPreconditionIndices, ref pathNodeSpecifiedPreconditions);
        }

        public void SetRewardSum(ref NativeHashMap<int, float> rewardSum)
        {
            _goapLog.SetRewardSum(ref rewardSum);
        }

        public void Log(string log)
        {
            // Debug.Log(log);
        }

        public void LogDone()
        {
            _onLogDone?.Invoke(_goapLog);
        }
    }
}