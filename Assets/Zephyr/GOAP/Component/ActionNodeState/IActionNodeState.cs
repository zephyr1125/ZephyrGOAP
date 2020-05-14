using Unity.Entities;

namespace Zephyr.GOAP.Component.ActionNodeState
{
    public interface IActionNodeState
    {
        Entity AgentEntity { get; set; }
    }
}