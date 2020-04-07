using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage.GoalState
{
    /// <summary>
    /// 表示一个goal正在被执行中
    /// </summary>
    public struct ExecutingGoal : IComponentData, IGoalState
    {
        public float Time { get; set; }
    }
}