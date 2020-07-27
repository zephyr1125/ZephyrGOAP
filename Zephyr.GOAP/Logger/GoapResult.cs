using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class GoapResult
    {
        public List<NodeLog> nodes;

        public List<EdgeLog> edges;

        public List<StateLog> baseStates;

        public string timeStart;
        
        public string timeCost;

        private DateTime _timeStart;

        private int[] _pathHash;

        private int _goalNodeHash;

        public List<NodeDependencyLog> pathDependencies;

        public bool isPlanSuccess;

        public void StartLog(EntityManager entityManager)
        {
            _timeStart = DateTime.Now;
            timeStart = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            
            nodes = new List<NodeLog>();
            edges = new List<EdgeLog>();
            baseStates = new List<StateLog>();
            pathDependencies = new List<NodeDependencyLog>();
        }
        
        public void SetNodeGraph(NodeGraph nodeGraph, EntityManager entityManager)
        {
            //转换所有node
            var nodesData = nodeGraph.GetNodes(Allocator.Temp);
            foreach (var node in nodesData)
            {
                var nodeLog = new NodeLog(nodeGraph, entityManager, node);
                if (node.HashCode.Equals(nodeGraph.StartNodeHash) ||
                    node.HashCode.Equals(nodeGraph.GoalNodeHash))
                {
                    nodeLog.isPath = true;
                }
                nodes.Add(nodeLog);
            }
            //转换所有edge
            var edgesData = nodeGraph.GetEdges(Allocator.Temp);
            foreach (var edge in edgesData)
            {
                edges.Add(new EdgeLog(edge));
            }

            _goalNodeHash = nodeGraph.GetGoalNode().HashCode;
            
            nodesData.Dispose();
        }

        public void SetPathResult(EntityManager entityManager,
            NativeArray<Entity> pathEntities, NativeList<Node> pathNodes)
        {
            _pathHash = new int[pathNodes.Length];
            for (var i = 0; i < pathNodes.Length; i++)
            {
                var node = pathNodes[i];
                var nodeEntity = pathEntities[i];//node与entity数组并列
                _pathHash[i] = node.HashCode;

                for (var nodeId = 0; nodeId < nodes.Count; nodeId++)
                {
                    var nodeLog = nodes[nodeId];
                    if (nodeLog.hashCode != node.HashCode) continue;
                    nodeLog.isPath = true;
                    nodeLog.pathNodeEntity = nodeEntity;    
                    nodeLog.estimateNavigateStartTime = node.EstimateStartTime;
                    nodeLog.navigationSubject =
                        new EntityLog(entityManager, node.NavigatingSubject);
                    break;
                }

                var bufferDependencies = entityManager.GetBuffer<NodeDependency>(pathEntities[i]);
                for (var j = 0; j < bufferDependencies.Length; j++)
                {
                    var dependencyEntity = bufferDependencies[j].Entity;
                    var dependencyId = pathEntities.IndexOf<Entity>(dependencyEntity);
                    pathDependencies.Add(new NodeDependencyLog(
                        node.HashCode, pathNodes[dependencyId].HashCode));
                }
            }

            timeCost = (DateTime.Now - _timeStart).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        }

        public void SetSpecifiedPreconditions(EntityManager entityManager,
            NativeList<int> pathNodeSpecifiedPreconditionIndices,
            NativeList<State> pathNodeSpecifiedPreconditions)
        {
            foreach (var nodeLog in nodes)
            {
                nodeLog.SetSpecifiedPreconditions(entityManager,
                    pathNodeSpecifiedPreconditionIndices, pathNodeSpecifiedPreconditions);
            }
        }

        public void SetNodeAgentInfos(EntityManager entityManager, NativeMultiHashMap<int, NodeAgentInfo> nodeTimes)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                // Assert.IsTrue(nodeTimes.ContainsKey(node.hashCode));
                if (!nodeTimes.ContainsKey(node.hashCode))
                {
                    Debug.Log("No agent info in node");
                }
                node.SetAgentInfo(entityManager, nodeTimes.GetValuesForKey(node.hashCode));
            }
        }

        public void SetNodeTotalTimes(NativeHashMap<int, float> nodeTotalTimes)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!nodeTotalTimes.ContainsKey(node.hashCode)) continue;
                node.SetTotalTime(nodeTotalTimes[node.hashCode]);
            }
        }

        public void SetBaseStates(StateGroup baseStates, EntityManager entityManager)
        {
            foreach (var baseState in baseStates)
            {
                this.baseStates.Add(new StateLog(entityManager, baseState));
            }
        }

        public void SetRewardSum(NativeHashMap<int, float> rewardSum)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!rewardSum.ContainsKey(node.hashCode)) continue;
                node.rewardSum = rewardSum[node.hashCode];
            }
        }

        public NodeLog[] GetPathResult()
        {
            var pathResult = new NodeLog[_pathHash.Length];
            for (var i = 0; i < _pathHash.Length; i++)
            {
                var node = nodes.Find(n=>n.hashCode==_pathHash[i]);
                pathResult[i] = node;
            }

            return pathResult;
        }

        public NodeLog[] GetChildren(NodeLog parent)
        {
            var result = new List<NodeLog>();
            var hashCodes = new List<int>();
            var parentHash = parent.hashCode;
            foreach (var edge in edges)
            {
                if (edge.parentHash != parentHash) continue;
                hashCodes.Add(edge.childHash);
            }

            foreach (var hashCode in hashCodes)
            {
                result.Add(nodes.Find(node => node.hashCode == hashCode));
            }
            
            return result.ToArray();
        }

        public NodeLog GetGoalNodeLog()
        {
            return nodes.Find(log => log.hashCode == _goalNodeHash);
        }

        public void SetPlanSuccess(bool isSuccess)
        {
            isPlanSuccess = isSuccess;
        }
    }
}