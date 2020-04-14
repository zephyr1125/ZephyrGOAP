using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Entities;
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

        public void StartLog(EntityManager entityManager)
        {
            _timeStart = DateTime.Now;
            timeStart = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            
            nodes = new List<NodeLog>();
            edges = new List<EdgeLog>();
            currentStates = new List<StateLog>();
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

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            _pathHash = new int[pathResult.Length];
            for (var i = 0; i < pathResult.Length; i++)
            {
                var node = pathResult[i];
                _pathHash[i] = node.HashCode;
                
                foreach (var nodeLog in nodes)
                {
                    if (nodeLog.hashCode != node.HashCode) continue;
                    nodeLog.isPath = true;
                    break;
                }
            }

            timeCost = (DateTime.Now - _timeStart).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        }
        
        public void SetNodeTimes(EntityManager entityManager, ref NativeMultiHashMap<int, NodeTime> nodeTimes)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!nodeTimes.ContainsKey(node.hashCode)) continue;
                node.SetAgentTotalTime(entityManager, nodeTimes.GetValuesForKey(node.hashCode));
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