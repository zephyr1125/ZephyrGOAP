using System;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
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

        public void CheckForSameHash()
        {
            Debug.Log("Check Log Hashes...");
            foreach (var result in results)
            {
                for (var nodeId = 0; nodeId < result.nodes.Count; nodeId++)
                {
                    var node = result.nodes[nodeId];
                    for (var otherId = nodeId+1; otherId < result.nodes.Count; otherId++)
                    {
                        var other = result.nodes[otherId];
                        Assert.IsFalse(node.hashCode.Equals(other.hashCode), "Same hash in nodes!");
                    }
                }
            }
            Debug.Log("All node hashes not same");
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

        public void SetNodeGraph(NodeGraph nodeGraph, EntityManager entityManager)
        {
            _currentResult.SetNodeGraph(nodeGraph, entityManager);
        }

        public void SetPathResult(EntityManager entityManager,
            NativeArray<Entity> pathEntities, NativeList<Node> pathResult)
        {
            _currentResult.SetPathResult(entityManager, pathEntities, pathResult);
        }

        public void SetNodeAgentInfos(EntityManager entityManager,
            NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos)
        {
            _currentResult.SetNodeAgentInfos(entityManager, nodeAgentInfos);
        }

        public void SetNodeTotalTimes(NativeHashMap<int, float> nodeTotalTimes)
        {
            _currentResult.SetNodeTotalTimes(nodeTotalTimes);
        }

        public void SetBaseStates(StateGroup baseStates, EntityManager entityManager)
        {
            _currentResult.SetBaseStates(baseStates, entityManager);
        }

        public void SetSpecifiedPreconditions(EntityManager entityManager,
            NativeList<int> pathNodeSpecifiedPreconditionIndices,
            NativeList<State> pathNodeSpecifiedPreconditions)
        {
            _currentResult.SetSpecifiedPreconditions(entityManager,
                pathNodeSpecifiedPreconditionIndices, pathNodeSpecifiedPreconditions);
        }

        public NodeLog GetGoalNodeView()
        {
            return _currentResult.GetGoalNodeLog();
        }

        public NodeLog[] GetPathResult()
        {
            return _currentResult.GetPathResult();
        }

        public NodeLog[] GetChildren(NodeLog parent)
        {
            return _currentResult.GetChildren(parent);
        }

        public void SetRewardSum(NativeHashMap<int, float> rewardSum)
        {
            _currentResult.SetRewardSum(rewardSum);
        }

        public void SetPlanSuccess(bool isSuccess)
        {
            _currentResult.SetPlanSuccess(isSuccess);
        }

        public bool IsPlanSuccess()
        {
            return _currentResult.isPlanSuccess;
        }
    }

}