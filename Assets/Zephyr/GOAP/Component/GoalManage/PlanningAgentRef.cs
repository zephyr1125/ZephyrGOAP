using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage
{
    /// <summary>
    /// 表示一个goal正在被一个agent规划中
    /// </summary>
    public struct PlanningAgentRef : IComponentData
    {
        public Entity agentEntity;
    }
}