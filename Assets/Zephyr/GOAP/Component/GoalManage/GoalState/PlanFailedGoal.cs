using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage.GoalState
{
    public struct PlanFailedGoal : IComponentData
    {
        public Entity AgentEntity;
        public double FailTime;
    }
}