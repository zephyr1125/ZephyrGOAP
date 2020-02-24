using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage.GoalState
{
    public struct PlanFailedGoal : IComponentData, IGoalState
    {
        public Entity AgentEntity { get; set; }
        public float Time { get; set; }
    }
}