using Unity.Entities;

namespace Zephyr.GOAP.Component.AgentState
{
    public struct ReadyToNavigate : IComponentData, IAgentState
    {
        public Entity NodeEntity { get; set; }
    }
}