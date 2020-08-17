using Unity.Entities;
using Zephyr.GOAP.Sample.Game.Component.Order;

namespace Zephyr.GOAP.Sample.Game.System
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class OrderCleanSystem : SystemBase
    {
        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var handle = Entities.WithName("OrderCleanJob")
                .ForEach((Entity entity, int entityInQueryIndex, in Order order) =>
                {
                    if (order.Amount > 0) return;
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }).ScheduleParallel(Dependency);
            EcbSystem.AddJobHandleForProducer(handle);
        }
    }
}