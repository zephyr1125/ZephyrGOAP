using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
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

        public void SetNodeGraph(NodeGraph nodeGraph, EntityManager entityManager)
        {
            _goapLog.SetNodeGraph(nodeGraph, entityManager);
        }

        public void SetPathResult(EntityManager entityManager,
            NativeArray<Entity> pathEntities, NativeList<Node> pathResult)
        {
            _goapLog.SetPathResult(entityManager, pathEntities, pathResult);
        }

        public void SetNodeAgentInfos(EntityManager entityManager,
            NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos)
        {
            _goapLog.SetNodeAgentInfos(entityManager, nodeAgentInfos);
        }

        public void SetNodeTotalTimes(NativeHashMap<int, float> nodeTimes)
        {
            _goapLog.SetNodeTotalTimes(nodeTimes);
        }

        public void SetBaseStates(StateGroup baseStates, EntityManager entityManager)
        {
            _goapLog.SetBaseStates(baseStates, entityManager);
        }

        public void SetSpecifiedPreconditions(EntityManager entityManager,
            NativeList<int> pathNodeSpecifiedPreconditionIndices,
            NativeList<State> pathNodeSpecifiedPreconditions)
        {
            _goapLog.SetSpecifiedPreconditions(entityManager,
                pathNodeSpecifiedPreconditionIndices, pathNodeSpecifiedPreconditions);
        }

        public void SetRewardSum(NativeHashMap<int, float> rewardSum)
        {
            _goapLog.SetRewardSum(rewardSum);
        }

        public void Log(string log)
        {
            // Debug.Log(log);
        }
        
        public void LogWarning(string log)
        {
            Debug.LogWarning(log);
        }

        public void LogDone()
        {
            _onLogDone?.Invoke(_goapLog);
        }

        public void LogPerformance(string log)
        {
            // Debug.Log(log);
        }

        public GoapLog GetLog()
        {
            return _goapLog;
        }

        public void SetWriteFile(bool isWriteFile)
        {
            //todo 尚不支持
        }

        public void SetPlanSuccess(bool isSuccess)
        {
            _goapLog.SetPlanSuccess(isSuccess);
        }

        public bool IsPlanSuccess()
        {
            return _goapLog.IsPlanSuccess();
        }
    }
}