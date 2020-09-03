using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component.Order
{
    public struct OrderExecuteTime : IComponentData
    {
        public float ExecutePeriod;
        public double StartTime;
    }
}