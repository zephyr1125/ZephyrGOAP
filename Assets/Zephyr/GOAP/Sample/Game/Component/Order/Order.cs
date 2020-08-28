using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component.Order
{
    /// <summary>
    /// 用于生产/采集设施的"订单"
    /// </summary>
    public struct Order : IComponentData
    {
        public Entity ExecutorEntity;
        public Entity FacilityEntity;
        public FixedString32 ItemName;
        public int Amount;
    }
}