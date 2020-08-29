using Unity.Entities;

namespace Zephyr.GOAP.Sample.GoapImplement.Component
{
    /// <summary>
    /// 位于Agent上，表示其监控的order，一对多
    /// </summary>
    public struct WatchingOrder : IBufferElementData
    {
        public Entity OrderEntity;
    }
}