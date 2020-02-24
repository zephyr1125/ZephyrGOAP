using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage.GoalState
{
    /// <summary>
    /// 表示一个goal正在被一个agent执行中
    /// </summary>
    public struct ExecutingGoal : IComponentData, IGoalState
    {
        public Entity AgentEntity { get; set; }
        public float Time { get; set; }
    }
}