using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.SensorSystem
{
    /// <summary>
    /// 探测所有原料源情况
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class RawSourceSensorSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(RawSourceTrait))]
        private struct SenseJob : IJobForEachWithEntity_EBCC<ContainedItemRef, ItemContainer, Translation>
        {
            [NativeDisableContainerSafetyRestriction, WriteOnly]
            public BufferFromEntity<State> States;

            public Entity CurrentStatesEntity;

            public void Execute(Entity entity, int jobIndex,
                DynamicBuffer<ContainedItemRef> itemRefs, ref ItemContainer container, ref Translation translation)
            {
                var buffer = States[CurrentStatesEntity];
                foreach (var itemRef in itemRefs)
                {
                    buffer.Add(new State
                    {
                        Target = entity,
                        Position = translation.Value,
                        Trait = typeof(RawSourceTrait),
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