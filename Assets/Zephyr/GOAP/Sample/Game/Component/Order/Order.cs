using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component.Order
{
    /// <summary>
    /// 用于生产类建筑的"订单"
    /// </summary>
    public struct Order : IComponentData
    {
        public Entity ExecutorEntity;
        public Entity FacilityEntity;
        public FixedString32 OutputName;
        public byte Amount;
    }
}