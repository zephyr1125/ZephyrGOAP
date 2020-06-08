using System;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP
{
    public static class Utils
    {
        public const string RoastPeachName = "roast_peach";
        public const string RawPeachName = "raw_peach";
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
            ref EntityCommandBuffer.Concurrent eCBuffer, Entity nodeEntity) 
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
            ref EntityCommandBuffer eCBuffer, Entity nodeEntity) 
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
            ref EntityCommandBuffer.Concurrent eCBuffer, Entity agentEntity) 
            where T : struct, IComponentData, IActionNodeState where TU : struct, IComponentData, IActionNodeState
        {
            eCBuffer.RemoveComponent<T>(jobIndex, actionNodeEntity);
            eCBuffer.AddComponent(jobIndex, actionNodeEntity, new TU{AgentEntity = agentEntity});
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
        public static NativeList<NativeString32> GetItemNamesOfSpecificTrait(ComponentType trait,
            Allocator allocator)
        {
            var result = new NativeList<NativeString32>(allocator);
            
            if (trait.Equals(typeof(FoodTrait)))
            {
                result.Add(RawPeachName);
                result.Add(RoastPeachName);
                result.Add("raw_apple");
                result.Add("roast_apple");
                result.Add("feast");
            }

            return result;
        }

        public static float RawPeachStamina = 0.2f;
        public static float RawAppleStamina = 0.3f;
        public static float RoastPeachStamina = 0.4f;
        public static float RoastAppleStamina = 0.5f;
        public static float FeastStamina = 2;
        
        /// <summary>
        /// 示例用的方法，获取不同食物的食用reward
        /// </summary>
        /// <param name="foodName"></param>
        /// <returns></returns>
        public static float GetFoodReward(NativeString32 foodName)
        {
            var plus = 10;
            switch (foodName.ToString())
            {
                case Utils.RawPeachName : return RawPeachStamina*plus;
                case Utils.RoastPeachName : return RoastPeachStamina*plus;
                case "raw_apple" : return RawAppleStamina*plus;
                case "roast_apple" : return RoastAppleStamina*plus;
                case "feast" : return FeastStamina * plus;
                default: return 0;
            }
        }
        
        public static float GetFoodStamina(NativeString32 foodName)
        {
            switch (foodName.ToString())
            {
                case Utils.RawPeachName : return RawPeachStamina;
                case Utils.RoastPeachName : return RoastPeachStamina;
                case "raw_apple" : return RawAppleStamina;
                case "roast_apple" : return RoastAppleStamina;
                case "feast" : return FeastStamina;
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