using DOTS.Component;
using DOTS.Component.AgentState;
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
    }
}