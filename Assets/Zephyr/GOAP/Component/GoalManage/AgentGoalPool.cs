using Unity.Entities;

namespace Zephyr.GOAP.Component.Goal
{
    public struct AgentGoalPool : IComponentData
    {
        public Entity Agent;
    }
}