using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class NodeTimeLog : IComparable<NodeTimeLog>
    {
        public EntityLog agentEntity;
        public float3 endPosition;
        public float totalTime;
        
        [SerializeField]
        private bool isNodeExecutor;

        public NodeTimeLog(EntityManager entityManager, NodeTime nodeTime, EntityLog executorAgentEntity)
        {
            agentEntity = new EntityLog(entityManager, nodeTime.AgentEntity);
            endPosition = nodeTime.EndPosition;
            totalTime = nodeTime.TotalTime;
            isNodeExecutor = agentEntity.Equals(executorAgentEntity);
        }

        public static NodeTimeLog[] CreateNodeTimeLogs(EntityManager entityManager, EntityLog executorAgentEntity,
            NativeMultiHashMap<int, NodeTime>.Enumerator nodeTimes)
        {
            var nodeTimeLogs = new List<NodeTimeLog>();
            foreach (var nodeTime in nodeTimes)
            {
                nodeTimeLogs.Add(new NodeTimeLog(entityManager, nodeTime, executorAgentEntity));
            }

            return nodeTimeLogs.ToArray();
        }

        public override string ToString()
        {
            var colorPrefix = "";
            var colorSuffix = "";
            if (isNodeExecutor)
            {
                //not supported before end of 2020
                // colorPrefix = "<color=#FF00FFFF>";
                // colorSuffix = "</color>";
                colorPrefix = "[";
                colorSuffix = "]";
            }
            return $"{colorPrefix}{totalTime}{colorSuffix}";
        }

        public int CompareTo(NodeTimeLog other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return agentEntity.index.CompareTo(other.agentEntity.index);
        }
    }
}