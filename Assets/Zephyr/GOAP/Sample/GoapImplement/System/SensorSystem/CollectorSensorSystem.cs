using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System.SensorManage;

namespace Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem
{
    /// <summary>
    /// 检测世界里的Collector，写入其存在
    /// 并且检测范围内的原料以写入潜在物品
    /// </summary>
    [UpdateAfter(typeof(RawSourceSensorSystem))]
    public class CollectorSensorSystem : SensorSystemBase
    {
        //todo 示例固定数值
        public const float CollectorRange = 100;

        private EntityQuery _rawQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _rawQuery = GetEntityQuery(
                ComponentType.ReadOnly<RawSourceTrait>(),
                ComponentType.ReadOnly<Translation>());
        }

        protected override JobHandle ScheduleSensorJob(JobHandle inputDeps,
            EntityCommandBuffer.ParallelWriter ecb, Entity baseStateEntity)
        {
            var raws = _rawQuery.ToComponentDataArray<RawSourceTrait>(Allocator.TempJob);
            var rawTranslations = _rawQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            
            return Entities.WithAll<CollectorTrait>()
                .WithReadOnly(raws)
                .WithReadOnly(rawTranslations)
                .WithDisposeOnCompletion(raws)
                .WithDisposeOnCompletion(rawTranslations)
                .ForEach(
                    (Entity collectorEntity, int entityInQueryIndex, in Translation translation) =>
                    {
                        var position = translation.Value;

                        //写入collector
                        ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                        {
                            Target = collectorEntity,
                            Position = position,
                            Trait = TypeManager.GetTypeIndex<CollectorTrait>(),
                        });

                        //基于附近原料，写入潜在物品源
                        for (var i = 0; i < raws.Length; i++)
                        {
                            if (math.distance(rawTranslations[i].Value, position) > CollectorRange) continue;
                            ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                            {
                                Target = collectorEntity,
                                Position = position,
                                Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                                ValueString = raws[i].RawName,
                                Amount = raws[i].Amount
                            });
                        }
                    }).Schedule(inputDeps);;
        }
    }
}