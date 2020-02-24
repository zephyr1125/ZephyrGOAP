using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage.GoalState
{
    public interface IGoalState
    {
        Entity AgentEntity { get; set; }
        float Time { get; set; }
    }
}