using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
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
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<ContainedItemRef>());
        }

        protected override JobHandle ScheduleSensorJob(JobHandle inputDeps,
            EntityCommandBuffer.ParallelWriter ecb, Entity baseStateEntity)
        {
            var rawEntities = _rawQuery.ToEntityArray(Allocator.TempJob);
            var raws = _rawQuery.ToComponentDataArray<RawSourceTrait>(Allocator.TempJob);
            var rawTranslations = _rawQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            var counts = GetComponentDataFromEntity<Count>(true);
            var containedItemRefs = GetBufferFromEntity<ContainedItemRef>(true);
            
            return Entities.WithAll<CollectorTrait>()
                .WithReadOnly(raws)
                .WithReadOnly(rawTranslations)
                .WithReadOnly(counts)
                .WithReadOnly(containedItemRefs)
                .WithDisposeOnCompletion(rawEntities)
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
                        var tempStates = new StateGroup(1, Allocator.Temp);
                        for (var rawId = 0; rawId < raws.Length; rawId++)
                        {
                            if (math.distance(rawTranslations[rawId].Value, position) > CollectorRange) continue;

                            //找到raw的物品容器里的那个物品以确定数量
                            var bufferContainedItemRef = containedItemRefs[rawEntities[rawId]];
                            var itemEntity = bufferContainedItemRef[0].ItemEntity;

                            var newState = new State
                            {
                                Target = collectorEntity,
                                Position = position,
                                Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                                ValueString = raws[rawId].RawName,
                                Amount = counts[itemEntity].Value
                            };
                            
                            tempStates.OR(newState);
                        }

                        for (var i = 0; i < tempStates.Length(); i++)
                        {
                            var state = tempStates[i];
                            ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, state);
                        }
                        
                        tempStates.Dispose();
                        
                    }).Schedule(inputDeps);;
        }
    }
}