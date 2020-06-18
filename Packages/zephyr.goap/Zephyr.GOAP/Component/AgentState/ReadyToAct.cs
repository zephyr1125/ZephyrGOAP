using Unity.Entities;

namespace Zephyr.GOAP.Component.AgentState
{
    public struct ReadyToAct : IComponentData, IAgentState
    {
        public Entity NodeEntity { get; set; }
    }
}