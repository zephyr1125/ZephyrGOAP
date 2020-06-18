using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage.GoalState
{
    public struct PlanFailedGoal : IComponentData, IGoalState
    {
        public float Time { get; set; }
    }
}