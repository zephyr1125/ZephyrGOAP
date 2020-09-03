using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component.Order.OrderState
{
    public struct OrderExecuting : IComponentData, IOrderState
    {
        public float ExecutePeriod;
        public double StartTime;
    }
}