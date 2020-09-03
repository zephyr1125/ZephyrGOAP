using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;

namespace Zephyr.GOAP.Sample.Game.System.OrderSystem
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class OrderDependencyCleanSystem : SystemBase
    {
        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            Entities
                .WithoutBurst()
                .WithAll<OrderReadyToNavigate>()    //只在ReadyToNavigate状态下检查
                .WithAll<DependentOrder>()
                .ForEach((Entity entity, in DependentOrder dependentOrder) =>
                {
                    if (EntityManager.Exists(dependentOrder.dependentOrderEntity)) return;
                    ecb.RemoveComponent<DependentOrder>(entity);
                }).Run();
        }
    }
}