using Unity.Entities;

namespace Zephyr.GOAP.Component
{
    /// <summary>
    /// 位于Agent上，连接到其正在执行的Action Node
    /// </summary>
    public struct ExecutingAgent : IComponentData
    {
        public Entity NodeEntity;
    }
}