using Unity.Entities;

namespace Zephyr.GOAP.Component
{
    /// <summary>
    /// 表示一个action node正在被执行
    /// </summary>
    public struct ActionNodeActing : IComponentData
    {
        public Entity AgentEntity;
    }
}