using Unity.Entities;

namespace Zephyr.GOAP.Component
{
    /// <summary>
    /// 表示所在的node依赖于其他node先行完成
    /// </summary>
    public struct NodeDependency : IBufferElementData
    {
        public Entity Entity;
    }
}