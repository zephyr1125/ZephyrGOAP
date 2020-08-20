
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;

namespace Zephyr.GOAP.Sample.Game.System
{
    /// <summary>
    /// RawSource容器内的物品不可以在Authoring时创建，因此额外使用一个System处理
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class RawSourceItemCreationSystem : JobComponentSystem
    {
        private EntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDepth)
        {
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var handle = Entities.WithNone<ContainedItemRef>()
                .ForEach((Entity rawContainerEntity, int entityInQueryIndex,
                    in RawSourceTrait rawSourceTrait) =>
                {
                    var rawName = rawSourceTrait.RawName;
                    var itemEntity = ecb.CreateEntity(entityInQueryIndex);
                    ecb.AddComponent(entityInQueryIndex, itemEntity, new Item{});
                    ecb.AddComponent(entityInQueryIndex, itemEntity, new Name{Value = rawName});
                    ecb.AddComponent(entityInQueryIndex, itemEntity, new Count{Value = rawSourceTrait.InitialAmount});
            
                    ecb.AddComponent(entityInQueryIndex, rawContainerEntity, new ItemContainer {IsTransferSource = false});
                    var buffer = ecb.AddBuffer<ContainedItemRef>(entityInQueryIndex, rawContainerEntity);
                    buffer.Add(new ContainedItemRef{ItemEntity = itemEntity, ItemName = rawName});
                }).Schedule(inputDepth);
            _ecbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}