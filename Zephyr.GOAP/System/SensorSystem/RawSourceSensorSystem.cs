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
        private struct SenseJob : IJobForEachWithEntity_ECC<RawSourceTrait, Translation>
        {
            [NativeDisableContainerSafetyRestriction, WriteOnly]
            public BufferFromEntity<State> States;

            public Entity CurrentStatesEntity;

            public void Execute(Entity entity, int jobIndex,
                ref RawSourceTrait rawSourceTrait, ref Translation translation)
            {
                var buffer = States[CurrentStatesEntity];
                buffer.Add(new State
                {
                    Target = entity,
                    Position = translation.Value,
                    Trait = typeof(RawSourceTrait),
                    ValueString = rawSourceTrait.RawName
                });
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