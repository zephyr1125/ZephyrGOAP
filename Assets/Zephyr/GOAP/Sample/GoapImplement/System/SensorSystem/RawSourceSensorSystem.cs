using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem
{
    /// <summary>
    /// 探测所有原料源情况
    /// </summary>
    public class RawSourceSensorSystem : SensorSystemBase
    {
        protected override JobHandle ScheduleSensorJob(JobHandle inputDeps,
            EntityCommandBuffer.ParallelWriter ecb, Entity baseStateEntity)
        {
            return Entities.ForEach((Entity rawSourceEntity, int entityInQueryIndex,
                    in RawSourceTrait rawSourceTrait, in Translation translation) =>
            {
                ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                {
                    Target = rawSourceEntity,
                    Position = translation.Value,
                    Trait = TypeManager.GetTypeIndex<RawSourceTrait>(),
                    ValueString = rawSourceTrait.RawName
                });
            }).Schedule(inputDeps);
        }
    }
}