using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

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
        /// <param name="agent"></param>
        /// <param name="toNextNode"></param>
        /// <typeparam name="T">离开的状态</typeparam>
        /// <typeparam name="U">进入的状态</typeparam>
        public static void NextAgentState<T, U>(Entity agentEntity, int jobIndex,
            ref EntityCommandBuffer.Concurrent eCBuffer, Agent agent, bool toNextNode) 
            where T : struct, IComponentData, IAgentState where U : struct, IComponentData, IAgentState
        {
            eCBuffer.RemoveComponent<T>(jobIndex, agentEntity);
            eCBuffer.AddComponent<U>(jobIndex, agentEntity);
            if (toNextNode)
            {
                eCBuffer.SetComponent(jobIndex, agentEntity, new Agent
                {
                    ExecutingNodeId = agent.ExecutingNodeId+1
                });
            }
        }
        
        public static void NextAgentState<T, U>(Entity agentEntity,
            EntityManager entityManager, bool toNextNode) 
            where T : struct, IComponentData, IAgentState where U : struct, IComponentData, IAgentState
        {
            entityManager.RemoveComponent<T>(agentEntity);
            entityManager.AddComponent<U>(agentEntity);
            if (toNextNode)
            {
                var agent = entityManager.GetComponentData<Agent>(agentEntity);
                agent.ExecutingNodeId += 1;
                entityManager.SetComponentData(agentEntity, agent);
            }
        }

        public static void NextGoalState<T, U>(Entity agentEntity, Entity goalEntity, EntityManager entityManager,
            double time) where T : struct, IComponentData, IGoalState where U : struct, IComponentData, IGoalState
        {
            entityManager.RemoveComponent<T>(goalEntity);
            entityManager.AddComponentData(goalEntity,
                new U{AgentEntity = agentEntity, Time = (float)time});
            }

        /// <summary>
        /// 根据传入的配方输出筛选，传出其对应的输入State组
        /// </summary>
        /// <param name="currentStates"></param>
        /// <param name="recipeOutFilter"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static StateGroup GetRecipeInputInCurrentStates(ref StateGroup currentStates, State recipeOutFilter, Allocator allocator)
        {
            var result = new StateGroup(2, allocator);
            for (var i = 0; i < currentStates.Length(); i++)
            {
                if (currentStates[i].BelongTo(recipeOutFilter))
                {
                    result.Add(currentStates[i+1]);
                    result.Add(currentStates[i+2]);
                    break;
                }
            }

            return result;
        }
        
        /// <summary>
        /// 示例用的方法，根据trait获取所有对应物品名，实际应从define获取
        /// </summary>
        /// <param name="trait"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static NativeList<NativeString64> GetItemNamesOfSpecificTrait(ComponentType trait,
            Allocator allocator)
        {
            var result = new NativeList<NativeString64>(allocator);
            
            if (trait.Equals(typeof(FoodTrait)))
            {
                result.Add(new NativeString64("raw_peach"));
                result.Add(new NativeString64("roast_peach"));
                result.Add(new NativeString64("raw_apple"));
                result.Add(new NativeString64("roast_apple"));
            }

            return result;
        }

        public static float RawPeachStamina = 0.2f;
        public static float RawAppleStamina = 0.3f;
        public static float RoastPeachStamina = 0.4f;
        public static float RoastAppleStamina = 0.5f;
        
        /// <summary>
        /// 示例用的方法，获取不同食物的食用reward
        /// </summary>
        /// <param name="foodName"></param>
        /// <returns></returns>
        public static float GetFoodReward(NativeString64 foodName)
        {
            var plus = 10;
            switch (foodName.ToString())
            {
                case "raw_peach" : return RawPeachStamina*plus;
                case "roast_peach" : return RoastPeachStamina*plus;
                case "raw_apple" : return RawAppleStamina*plus;
                case "roast_apple" : return RoastAppleStamina*plus;
                default: return 0;
            }
        }
        
        public static float GetFoodStamina(NativeString64 foodName)
        {
            switch (foodName.ToString())
            {
                case "raw_peach" : return RawPeachStamina;
                case "roast_peach" : return RoastPeachStamina;
                case "raw_apple" : return RawAppleStamina;
                case "roast_apple" : return RoastAppleStamina;
                default: return 0;
            }
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
    }
}