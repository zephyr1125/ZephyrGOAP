using Unity.Entities;

namespace Zephyr.GOAP.Component.AgentState
{
    public struct ActDone : IComponentData, IAgentState
    {
        public Entity NodeEntity { get; set; }
    }
}