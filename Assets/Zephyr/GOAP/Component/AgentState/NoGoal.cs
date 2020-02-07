using Unity.Entities;

namespace Zephyr.GOAP.Component.AgentState
{
    /// <summary>
    /// 待机，没有任何goal的状态
    /// </summary>
    public struct NoGoal : IComponentData, IAgentState
    {
        
    }
}