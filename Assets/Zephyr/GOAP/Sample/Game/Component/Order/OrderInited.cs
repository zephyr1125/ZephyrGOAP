using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component.Order
{
    public struct OrderInited : IComponentData
    {
        public float ExecutePeriod;
        public double StartTime;
    }
}