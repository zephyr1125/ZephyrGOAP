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
    public class NodeAgentInfoLog : IComparable<NodeAgentInfoLog>
    {
        public EntityLog agentEntity;
        public float3 endPosition;
        public float navigateTime;
        public float executeTime;
        public float availableTime;
        
        [SerializeField]
        private bool isNodeExecutor;

        public NodeAgentInfoLog(EntityManager entityManager, NodeAgentInfo nodeAgentInfo, EntityLog executorAgentEntity)
        {
            agentEntity = new EntityLog(entityManager, nodeAgentInfo.AgentEntity);
            endPosition = nodeAgentInfo.EndPosition;
            navigateTime = nodeAgentInfo.NavigateTime;
            executeTime = nodeAgentInfo.ExecuteTime;
            availableTime = nodeAgentInfo.AvailableTime;
            isNodeExecutor = agentEntity.Equals(executorAgentEntity);
        }

        public static NodeAgentInfoLog[] CreateNodeAgentInfoLogs(EntityManager entityManager, EntityLog executorAgentEntity,
            NativeMultiHashMap<int, NodeAgentInfo>.Enumerator nodeTimes)
        {
            var nodeTimeLogs = new List<NodeAgentInfoLog>();
            foreach (var nodeTime in nodeTimes)
            {
                nodeTimeLogs.Add(new NodeAgentInfoLog(entityManager, nodeTime, executorAgentEntity));
            }

            return nodeTimeLogs.ToArray();
        }

        public override string ToString()
        {
            var position = $"({endPosition.x},{endPosition.y},{endPosition.z})";
            return $"[{agentEntity}]{navigateTime}+{executeTime}/{availableTime}{position}";
        }

        public int CompareTo(NodeAgentInfoLog other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return agentEntity.index.CompareTo(other.agentEntity.index);
        }
    }
}