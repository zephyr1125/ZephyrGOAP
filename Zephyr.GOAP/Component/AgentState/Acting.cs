using Unity.Entities;

namespace Zephyr.GOAP.Component.AgentState
{
    public struct Acting : IComponentData, IAgentState
    {
        public Entity NodeEntity { get; set; }
    }
}