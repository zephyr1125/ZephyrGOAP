using System;
using System.Collections.Generic;
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
            states = StateLog.CreateStateViews(entityManager, nodeGraph.GetNodeStates(node));
            preconditions = StateLog.CreateStateViews(entityManager, nodeGraph.GetNodePreconditions(node));
            effects = StateLog.CreateStateViews(entityManager, nodeGraph.GetNodeEffects(node));
            hashCode = node.HashCode;
        }
    }
}