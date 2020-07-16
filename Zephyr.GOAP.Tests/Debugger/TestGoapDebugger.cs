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
        private GoapLog _goapLog;

        public void StartLog(EntityManager entityManager)
        {
            if (_goapLog == null)
            {
                _goapLog = new GoapLog();
            }
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

        public void SetBaseStates(ref StateGroup baseStates, EntityManager entityManager)
        {
            _goapLog.SetBaseStates(ref baseStates, entityManager);
        }

        public void SetRewardSum(ref NativeHashMap<int, float> rewardSum)
        {
            _goapLog.SetRewardSum(ref rewardSum);
        }

        public void SetSpecifiedPreconditions(EntityManager entityManager,
            ref NativeList<int> pathNodeSpecifiedPreconditionIndices,
            ref NativeList<State> pathNodeSpecifiedPreconditions)
        {
            _goapLog.SetSpecifiedPreconditions(entityManager,
                ref pathNodeSpecifiedPreconditionIndices, ref pathNodeSpecifiedPreconditions);
        }

        public void LogDone()
        {
            SaveToFile();
        }

        private void SaveToFile()
        {
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

        public void Dispose()
        {
            
        }
    }
}