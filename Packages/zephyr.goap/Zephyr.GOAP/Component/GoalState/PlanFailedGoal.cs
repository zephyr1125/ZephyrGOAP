using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalState
{
    public struct PlanFailedGoal : IComponentData, IGoalState
    {
        public float Time { get; set; }
    }
}