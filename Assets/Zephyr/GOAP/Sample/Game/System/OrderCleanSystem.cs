using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Sample.Game.Component.Order;

namespace Zephyr.GOAP.Sample.Game.System
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class OrderCleanSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var handle = Entities.WithName("OrderCleanJob")
                .ForEach((Entity entity, int entityInQueryIndex, in Order order) =>
                {
                    if (order.Amount > 0) return;
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }).Schedule(inputDeps);
            EcbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}