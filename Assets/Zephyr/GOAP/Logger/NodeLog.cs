using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
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

        public StateLog[] states, preconditions, effects;

        public bool isPath;
        
        public int hashCode;

        /// <summary>
        /// 在绘制Node Graph时的位置
        /// </summary>
        [NonSerialized]
        public Vector2 DrawPos;

        public NodeLog(ref NodeGraph nodeGraph, EntityManager entityManager, Node node)
        {
            name = node.Name.ToString();
            iteration = node.Iteration;
            reward = node.Reward;
            navigationSubject = new EntityLog(entityManager, node.NavigatingSubject);
            agentExecutorEntity = new EntityLog(entityManager, node.AgentExecutorEntity);
            states = StateLog.CreateStateLogs(entityManager, nodeGraph.GetNodeStates(node));
            preconditions = StateLog.CreateStateLogs(entityManager, nodeGraph.GetNodePreconditions(node));
            effects = StateLog.CreateStateLogs(entityManager, nodeGraph.GetNodeEffects(node));
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

        /// <summary>
        /// 各agent信息
        /// </summary>
        /// <returns></returns>
        public string[] NodeAgentInfos()
        {
            var sorted = new SortedSet<NodeAgentInfoLog>(nodeAgentInfos);
            var texts = new List<string>();
            foreach (var log in sorted)
            {
                texts.Add(log.ToString());
            }

            return texts.ToArray();
        }
    }
}