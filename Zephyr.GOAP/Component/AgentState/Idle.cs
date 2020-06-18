using Unity.Entities;

namespace Zephyr.GOAP.Component.AgentState
{
    public struct Idle : IComponentData, IAgentState
    {
        public Entity NodeEntity { get; set; }
    }
}