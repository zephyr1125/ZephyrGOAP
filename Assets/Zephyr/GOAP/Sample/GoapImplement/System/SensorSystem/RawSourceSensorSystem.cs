using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System.SensorManage;

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
            var counts = GetComponentDataFromEntity<Count>(true);
            return Entities
                .WithReadOnly(counts)
                .ForEach((Entity rawSourceEntity, int entityInQueryIndex,
                    in DynamicBuffer<ContainedItemRef> containedItemRefs,
                    in RawSourceTrait rawSourceTrait, in Translation translation) =>
                {
                    var itemEntity = containedItemRefs[0].ItemEntity;
                    
                    ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                    {
                        Target = rawSourceEntity,
                        Position = translation.Value,
                        Trait = TypeManager.GetTypeIndex<RawSourceTrait>(),
                        ValueString = rawSourceTrait.RawName,
                        Amount = counts[itemEntity].Value
                    });
                }).Schedule(inputDeps);
        }
    }
}