using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class GoapResult
    {
        public List<NodeLog> nodes;

        public List<EdgeLog> edges;

        public List<StateLog> currentStates;

        public string timeStart;
        
        public string timeCost;

        private DateTime _timeStart;

        private int[] _pathHash;

        public List<NodeDependencyLog> pathDependencies;

        public void StartLog(EntityManager entityManager)
        {
            _timeStart = DateTime.Now;
            timeStart = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            
            nodes = new List<NodeLog>();
            edges = new List<EdgeLog>();
            currentStates = new List<StateLog>();
            pathDependencies = new List<NodeDependencyLog>();
        }
        
        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            //转换所有node
            var nodesData = nodeGraph.GetNodes(Allocator.Temp);
            foreach (var node in nodesData)
            {
                nodes.Add(new NodeLog(ref nodeGraph, entityManager, node));
            }
            //转换所有edge
            var edgesData = nodeGraph.GetEdges(Allocator.Temp);
            foreach (var edge in edgesData)
            {
                edges.Add(new EdgeLog(edge));
            }

            nodesData.Dispose();
        }

        public void SetPathResult(EntityManager entityManager,
            ref NativeArray<Entity> pathEntities, ref NativeList<Node> pathNodes)
        {
            _pathHash = new int[pathNodes.Length];
            for (var i = 0; i < pathNodes.Length; i++)
            {
                var node = pathNodes[i];
                _pathHash[i] = node.HashCode;
                
                foreach (var nodeLog in nodes)
                {
                    if (nodeLog.hashCode != node.HashCode) continue;
                    nodeLog.isPath = true;
                    nodeLog.estimateNavigateStartTime = node.EstimateStartTime;
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
        
        public void SetNodeAgentInfos(EntityManager entityManager, ref NativeMultiHashMap<int, NodeAgentInfo> nodeTimes)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!nodeTimes.ContainsKey(node.hashCode)) continue;
                node.SetAgentInfo(entityManager, nodeTimes.GetValuesForKey(node.hashCode));
            }
        }

        public void SetNodeTotalTimes(ref NativeHashMap<int, float> nodeTotalTimes)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!nodeTotalTimes.ContainsKey(node.hashCode)) continue;
                node.SetTotalTime(nodeTotalTimes[node.hashCode]);
            }
        }

        public void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager)
        {
            foreach (var currentState in currentStates)
            {
                this.currentStates.Add(new StateLog(entityManager, currentState));
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
    }
}