using Unity.Entities;

namespace Zephyr.GOAP.Component.AgentState
{
    public struct Navigating : IComponentData, IAgentState
    {
        public Entity NodeEntity { get; set; }
    }
}