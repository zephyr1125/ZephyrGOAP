using Unity.Entities;

namespace Zephyr.GOAP.Component.ActionNodeState
{
    /// <summary>
    /// 表示一个action node正在被执行
    /// </summary>
    public struct ActionNodeDone : IComponentData, IActionNodeState
    {
        public Entity AgentEntity { get; set; }
    }
}