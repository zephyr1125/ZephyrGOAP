using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS
{
    public class Utils
    {
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
                result.Add(new NativeString64("peach"));
                result.Add(new NativeString64("apple"));
            }

            return result;
        }
    }
}