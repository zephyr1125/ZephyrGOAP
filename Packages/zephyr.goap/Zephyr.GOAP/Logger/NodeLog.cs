using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class NodeLog
    {
        public string name;

        public int iteration;

        public float reward;

        /// <summary>
        /// 指各个agent到此节点时的累计时间
        /// </summary>
        public NodeAgentInfoLog[] nodeAgentInfos;

        public float totalTime, estimateNavigateStartTime;

        public EntityLog agentExecutorEntity, navigationSubject;

        public StateLog[] requires, preconditions, effects, deltas;

        public bool isPath;
        
        public int hashCode;

        /// <summary>
        /// 在绘制Node Graph时的位置
        /// </summary>
        [NonSerialized]
        public Vector2 DrawPos;

        public bool isDeadEnd;

        public float rewardSum;

        public Entity pathNodeEntity;

        public NodeLog(NodeGraph nodeGraph, EntityManager entityManager, Node node)
        {
            name = node.Name.ToString();
            iteration = node.Iteration;
            reward = node.Reward;
            navigationSubject = new EntityLog(entityManager, node.NavigatingSubject);
            agentExecutorEntity = new EntityLog(entityManager, node.AgentExecutorEntity);
            
            preconditions = StateLog.CreateStateLogs(entityManager, nodeGraph.GetPreconditions(node));
            effects = StateLog.CreateStateLogs(entityManager, nodeGraph.GetEffects(node));
            requires = StateLog.CreateStateLogs(entityManager, nodeGraph.GetRequires(node));
            deltas = StateLog.CreateStateLogs(entityManager, nodeGraph.GetDeltas(node));
            
            if (
                name.Equals("CookAction") &&
                effects.Length == 2)
            {
                Debug.LogError("[GoapLog] Too much effects!");
            }
            isDeadEnd = nodeGraph._deadEndNodeHashes.Contains(node.HashCode);
            hashCode = node.HashCode;
        }

        public void SetAgentInfo(EntityManager entityManager, NativeMultiHashMap<int,NodeAgentInfo>.Enumerator enumerator)
        {
            nodeAgentInfos = NodeAgentInfoLog.CreateNodeAgentInfoLogs(entityManager, agentExecutorEntity, enumerator);
        }

        public void SetTotalTime(float time)
        {
            totalTime = time;
        }

        public void SetSpecifiedPreconditions(EntityManager entityManager,
            NativeList<int> pathNodeSpecifiedPreconditionIndices,
            NativeList<State> pathNodeSpecifiedPreconditions)
        {
            if (!isPath) return;
            
            var newPreconditions = new List<StateLog>();
            for (var i = 0; i < pathNodeSpecifiedPreconditionIndices.Length; i++)
            {
                if (!hashCode.Equals(pathNodeSpecifiedPreconditionIndices[i])) continue;
                newPreconditions.Add(new StateLog(
                    entityManager, pathNodeSpecifiedPreconditions[i]));
            }

            preconditions = newPreconditions.ToArray();
        }

        /// <summary>
        /// 各agent信息
        /// </summary>
        /// <returns></returns>
        public string[] NodeAgentInfos()
        {
            var texts = new List<string>();
            if (nodeAgentInfos == null) return texts.ToArray();
            
            var sorted = new SortedSet<NodeAgentInfoLog>(nodeAgentInfos);
            foreach (var log in sorted)
            {
                texts.Add(log.ToString());
            }
            return texts.ToArray();
        }
    }
}