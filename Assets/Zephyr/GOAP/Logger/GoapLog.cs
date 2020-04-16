using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class GoapLog
    {
        public List<GoapResult> results;

        private GoapResult _currentLog;

        public GoapLog()
        {
            results = new List<GoapResult>();
        }

        public void StartLog(EntityManager entityManager)
        {
            _currentLog = new GoapResult();
            results.Add(_currentLog);
            _currentLog.StartLog(entityManager);
        }

        public GoapResult GetResult(int id)
        {
            return results[id];
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            _currentLog.SetNodeGraph(ref nodeGraph, entityManager);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            _currentLog.SetPathResult(ref pathResult);
        }

        public void SetNodeAgentInfos(EntityManager entityManager,
            ref NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos)
        {
            _currentLog.SetNodeAgentInfos(entityManager, ref nodeAgentInfos);
        }

        public void SetNodeTotalTimes(ref NativeHashMap<int, float> nodeTotalTimes)
        {
            _currentLog.SetNodeTotalTimes(ref nodeTotalTimes);
        }

        public void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager)
        {
            _currentLog.SetCurrentStates(ref currentStates, entityManager);
        }

        public NodeLog GetGoalNodeView()
        {
            return _currentLog.nodes[0];
        }

        public NodeLog[] GetPathResult()
        {
            return _currentLog.GetPathResult();
        }

        public NodeLog[] GetChildren(NodeLog parent)
        {
            return _currentLog.GetChildren(parent);
        }
    }

}