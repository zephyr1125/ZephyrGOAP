using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.SensorSystem
{
    /// <summary>
    /// 探测所有可拿取的物品容器的物品情况
    /// todo 可以考虑下是否有必要优化为只记录离agent最近的，节省内存牺牲运算
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class ItemSourceSensorSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(ItemContainerTrait))]
        private struct SenseJob : IJobForEachWithEntity_EBC<ContainedItemRef, ItemContainer>
        {
            [NativeDisableContainerSafetyRestriction, WriteOnly]
            public BufferFromEntity<State> States;

            public Entity CurrentStatesEntity;
            
            public void Execute(Entity entity, int jobIndex, 
                DynamicBuffer<ContainedItemRef> itemRefs, ref ItemContainer container)
            {
                if (!container.IsTransferSource) return;

                var buffer = States[CurrentStatesEntity];
                foreach (var itemRef in itemRefs)
                {
                    buffer.Add(new State
                    {
                        Target = entity,
                        Trait = typeof(ItemContainerTrait),
                        ValueString = itemRef.ItemName
                    });
                }
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new SenseJob
            {
                States = GetBufferFromEntity<State>(),
                CurrentStatesEntity = CurrentStatesHelper.CurrentStatesEntity
            };
            var handle = job.Schedule(this, inputDeps);
            return handle;
        }
    }
}