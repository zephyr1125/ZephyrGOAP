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

        private GoapResult _currentResult;

        public GoapLog()
        {
            results = new List<GoapResult>();
        }

        public void StartLog(EntityManager entityManager)
        {
            _currentResult = new GoapResult();
            results.Add(_currentResult);
            _currentResult.StartLog(entityManager);
        }

        public GoapResult GetResult(int id)
        {
            return results[id];
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            _currentResult.SetNodeGraph(ref nodeGraph, entityManager);
        }

        public void SetPathResult(EntityManager entityManager,
            ref NativeArray<Entity> pathEntities, ref NativeList<Node> pathResult)
        {
            _currentResult.SetPathResult(entityManager, ref pathEntities, ref pathResult);
        }

        public void SetNodeAgentInfos(EntityManager entityManager,
            ref NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos)
        {
            _currentResult.SetNodeAgentInfos(entityManager, ref nodeAgentInfos);
        }

        public void SetNodeTotalTimes(ref NativeHashMap<int, float> nodeTotalTimes)
        {
            _currentResult.SetNodeTotalTimes(ref nodeTotalTimes);
        }

        public void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager)
        {
            _currentResult.SetCurrentStates(ref currentStates, entityManager);
        }

        public void SetSpecifiedPreconditions(EntityManager entityManager,
            ref NativeList<int> pathNodeSpecifiedPreconditionIndices,
            ref NativeList<State> pathNodeSpecifiedPreconditions)
        {
            _currentResult.SetSpecifiedPreconditions(entityManager,
                ref pathNodeSpecifiedPreconditionIndices, ref pathNodeSpecifiedPreconditions);
        }

        public NodeLog GetGoalNodeView()
        {
            return _currentResult.nodes[0];
        }

        public NodeLog[] GetPathResult()
        {
            return _currentResult.GetPathResult();
        }

        public NodeLog[] GetChildren(NodeLog parent)
        {
            return _currentResult.GetChildren(parent);
        }
    }

}