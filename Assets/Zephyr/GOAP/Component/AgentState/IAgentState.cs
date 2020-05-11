using Unity.Entities;

namespace Zephyr.GOAP.Component.AgentState
{
    public interface IAgentState
    {
        Entity NodeEntity { get; set; }
    }
}