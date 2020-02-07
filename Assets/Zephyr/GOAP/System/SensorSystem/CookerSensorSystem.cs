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
    /// 检测世界里的Cooker，写入其存在
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class CookerSensorSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(CookerTrait))]
        private struct SenseJob : IJobForEachWithEntity_EB<ContainedOutput>
        {
            [NativeDisableContainerSafetyRestriction, WriteOnly]
            public BufferFromEntity<State> States;

            public Entity CurrentStatesEntity;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<ContainedOutput> recipes)
            {
                //写入cooker
                var buffer = States[CurrentStatesEntity];
                buffer.Add(new State
                {
                    Target = entity,
                    Trait = typeof(CookerTrait),
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