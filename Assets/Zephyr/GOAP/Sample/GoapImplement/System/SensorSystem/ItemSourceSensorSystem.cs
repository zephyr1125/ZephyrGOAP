using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem
{
    /// <summary>
    /// 探测所有可拿取的物品容器的物品情况
    /// todo 可以考虑下是否有必要优化为只记录离agent最近的，节省内存牺牲运算
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class ItemSourceSensorSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var baseStateEntity = BaseStatesHelper.BaseStatesEntity;
            
            var handle = Entities.WithAll<ItemContainerTrait>()
                .ForEach((Entity itemContainerEntity, int entityInQueryIndex,
                    DynamicBuffer<ContainedItemRef> itemRefs,
                    in ItemContainer itemContainer, in Translation translation) =>
                {
                    if (!itemContainer.IsTransferSource) return;
                    
                    foreach (var itemRef in itemRefs)
                    {
                        ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                        {
                            Target = itemContainerEntity,
                            Position = translation.Value,
                            Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
                            ValueString = itemRef.ItemName
                        });
                    }
                }).Schedule(inputDeps);
            EcbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}