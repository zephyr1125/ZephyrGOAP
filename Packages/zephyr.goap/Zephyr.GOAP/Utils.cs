using System;
using Unity.Entities;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalState;

namespace Zephyr.GOAP
{
    public static class Utils
    {
        public static float GoalMonitorSystemInterval = 1;

        /// <summary>
        /// Agent的状态机使用，进入下一个指定状态
        /// </summary>
        /// <param name="agentEntity"></param>
        /// <param name="jobIndex"></param>
        /// <param name="eCBuffer"></param>
        /// <param name="nodeEntity"></param>
        /// <typeparam name="T">离开的状态</typeparam>
        /// <typeparam name="TU">进入的状态</typeparam>
        public static void NextAgentState<T, TU>(Entity agentEntity, int jobIndex,
            EntityCommandBuffer.ParallelWriter eCBuffer, Entity nodeEntity) 
            where T : struct, IComponentData, IAgentState where TU : struct, IComponentData, IAgentState
        {
            eCBuffer.RemoveComponent<T>(jobIndex, agentEntity);
            eCBuffer.AddComponent(jobIndex, agentEntity, new TU{NodeEntity = nodeEntity});
        }
        
        public static void NextAgentState<T, U>(Entity agentEntity,
            EntityManager entityManager) 
            where T : struct, IComponentData, IAgentState where U : struct, IComponentData, IAgentState
        {
            entityManager.RemoveComponent<T>(agentEntity);
            entityManager.AddComponent<U>(agentEntity);
        }
        
        public static void NextAgentState<T, TU>(Entity agentEntity,
            EntityCommandBuffer eCBuffer, Entity nodeEntity) 
            where T : struct, IComponentData, IAgentState where TU : struct, IComponentData, IAgentState
        {
            eCBuffer.RemoveComponent<T>(agentEntity);
            eCBuffer.AddComponent(agentEntity, new TU{NodeEntity = nodeEntity});
        }

        public static void NextGoalState<T, U>(Entity goalEntity, EntityManager entityManager,
            double time) where T : struct, IComponentData, IGoalState where U : struct, IComponentData, IGoalState
        {
            entityManager.RemoveComponent<T>(goalEntity);
            entityManager.AddComponentData(goalEntity, new U{Time = (float)time});
        }
        
        public static void NextActionNodeState<T, TU>(Entity actionNodeEntity, int jobIndex,
            EntityCommandBuffer.ParallelWriter eCBuffer, Entity agentEntity) 
            where T : struct, IComponentData, IActionNodeState where TU : struct, IComponentData, IActionNodeState
        {
            eCBuffer.RemoveComponent<T>(jobIndex, actionNodeEntity);
            eCBuffer.AddComponent(jobIndex, actionNodeEntity, new TU{AgentEntity = agentEntity});
        }

        public static bool Any<T>(this DynamicBuffer<T> buffer, Func<T, bool> prediction)
            where T : struct, IBufferElementData
        {
            var any = false;
            for (var i = 0; i < buffer.Length; i++)
            {
                var element = buffer[i];
                if (!prediction(element)) continue;
                any = true;
                break;
            }

            return any;
        }

        public const int BasicHash = 5381;
        // public const int BasicHash = 17;

        public static int CombineHash(int a, int b)
        {
            return (a << 5) + a + b;
            // return a * 31 + b;
        }

        public static int GetEntityHash(Entity entity)
        {
            var hash = BasicHash;
            hash = CombineHash(hash, entity.Index);
            hash = CombineHash(hash, entity.Version);
            return hash;
        }
    }
}