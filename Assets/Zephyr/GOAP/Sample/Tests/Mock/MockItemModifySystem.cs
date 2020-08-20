using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Sample.Game.Component;

namespace Zephyr.GOAP.Sample.Tests.Mock
{
    [DisableAutoCreation]
    public class MockItemModifySystem : JobComponentSystem
    {
        public EntityCommandBufferSystem EcbSystem;
        
        public int Amount { set; get; }
        public FixedString32 ItemName { set; get; }
        

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var allCounts = GetComponentDataFromEntity<Count>(true);
            var itemName = ItemName;
            var amount = Amount;

            var handle = Entities.WithName("MockAddItemJob")
                .WithAll<ItemContainer>()
                .WithReadOnly(allCounts)
                .ForEach((Entity containerEntity, int entityInQueryIndex,
                    DynamicBuffer<ContainedItemRef> containedItemRefs) =>
                {
                    Utils.ModifyItemInContainer(entityInQueryIndex, ecb,
                        containerEntity, containedItemRefs, allCounts, itemName, amount);
                }).Schedule(inputDeps);
            EcbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}