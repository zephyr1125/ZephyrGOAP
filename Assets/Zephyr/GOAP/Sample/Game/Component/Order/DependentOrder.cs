using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component.Order
{
    /// <summary>
    /// 一个order所依赖的另一个order
    /// </summary>
    public struct DependentOrder : IComponentData
    {
        public Entity dependentOrderEntity;
    }
}