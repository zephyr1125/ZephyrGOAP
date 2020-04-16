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

        public float executeTime;

        /// <summary>
        /// 指各个agent到此节点时的累计时间
        /// </summary>
        public NodeTimeLog[] nodeTimeLogs;

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
            executeTime = node.ExecuteTime;
            navigationSubject = new EntityLog(entityManager, node.NavigatingSubject);
            agentExecutorEntity = new EntityLog(entityManager, node.AgentExecutorEntity);
            states = StateLog.CreateStateLogs(entityManager, nodeGraph.GetNodeStates(node));
            preconditions = StateLog.CreateStateLogs(entityManager, nodeGraph.GetNodePreconditions(node));
            effects = StateLog.CreateStateLogs(entityManager, nodeGraph.GetNodeEffects(node));
            hashCode = node.HashCode;
        }

        public void SetAgentTotalTime(EntityManager entityManager, NativeMultiHashMap<int,NodeTime>.Enumerator enumerator)
        {
            nodeTimeLogs = NodeTimeLog.CreateNodeTimeLogs(entityManager, agentExecutorEntity, enumerator);
        }

        /// <summary>
        /// 简化版的各agent时间信息
        /// </summary>
        /// <returns></returns>
        public string NodeTimesToString()
        {
            var sorted = new SortedSet<NodeTimeLog>(nodeTimeLogs);
            var text = new StringBuilder();
            foreach (var log in sorted)
            {
                text.Append(log);
                text.Append(",");
            }
            if(text.Length>0)text.Remove(text.Length - 1, 1);
            return text.ToString();
        }

        /// <summary>
        /// 完整版的各agent时间信息
        /// </summary>
        /// <returns></returns>
        public string[] NodeTimesFull()
        {
            var sorted = new SortedSet<NodeTimeLog>(nodeTimeLogs);
            var texts = new List<string>();
            foreach (var log in sorted)
            {
                texts.Add(log.ToStringFull());
            }

            return texts.ToArray();
        }
    }
}