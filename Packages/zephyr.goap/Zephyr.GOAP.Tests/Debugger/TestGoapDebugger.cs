using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Debugger;
using Zephyr.GOAP.Logger;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Tests.Debugger
{
    public class TestGoapDebugger : IGoapDebugger, IDisposable
    {
        private bool _isWriteFile = true;
        private GoapLog _goapLog;

        public void StartLog(EntityManager entityManager)
        {
            if (_goapLog == null)
            {
                _goapLog = new GoapLog();
            }
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

        public void SetRewardSum(NativeHashMap<int, float> rewardSum)
        {
            _goapLog.SetRewardSum(rewardSum);
        }

        public void SetSpecifiedPreconditions(EntityManager entityManager,
            NativeList<int> pathNodeSpecifiedPreconditionIndices,
            NativeList<State> pathNodeSpecifiedPreconditions)
        {
            _goapLog.SetSpecifiedPreconditions(entityManager,
                pathNodeSpecifiedPreconditionIndices, pathNodeSpecifiedPreconditions);
        }

        public void LogDone()
        {
            SaveToFile();
        }

        private void SaveToFile()
        {
            if (!_isWriteFile) return;
            
            var json = JsonUtility.ToJson(_goapLog);

            var path = "GoapTestLog/" + DateTime.Now.ToShortDateString();
            var fileName = DateTime.Now.ToFileTime() + ".json";
            Directory.CreateDirectory(path);
            var writer = File.CreateText(path + "/" + fileName);
            writer.Write(json);
            writer.Close();
        }

        public NodeLog GoalNodeLog => _goapLog.GetGoalNodeView();

        public NodeLog[] PathResult => _goapLog.GetPathResult();

        public NodeLog[] GetChildren(NodeLog parent)
        {
            return _goapLog.GetChildren(parent);
        }
        
        public void Log(string log)
        {
            // Debug.Log(log);
        }

        public void LogWarning(string log)
        {
            Debug.LogWarning(log);
        }

        public void LogPerformance(string log)
        {
            // Debug.Log(log);
        }

        public GoapLog GetLog()
        {
            return _goapLog;
        }

        public void Dispose()
        {
            
        }

        public void SetWriteFile(bool isWriteFile)
        {
            _isWriteFile = isWriteFile;
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