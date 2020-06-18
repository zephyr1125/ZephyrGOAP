using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage.GoalState
{
    /// <summary>
    /// 表示一个goal尚未被规划
    /// </summary>
    public struct IdleGoal : IComponentData, IGoalState
    {
        public float Time { get; set; }
    }
}