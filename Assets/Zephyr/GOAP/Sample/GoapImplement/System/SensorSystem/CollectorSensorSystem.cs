using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem
{
    /// <summary>
    /// 检测世界里的Collector，写入其存在
    /// 并且检测范围内的原料以写入潜在物品
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    [UpdateAfter(typeof(RawSourceSensorSystem))]
    public class CollectorSensorSystem : JobComponentSystem
    {
        //todo 示例固定数值
        public const float CollectorRange = 100;

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
            var bufferStates = GetBufferFromEntity<State>(true)[baseStateEntity];

            var handle = Entities.WithAll<CollectorTrait>()
                .WithReadOnly(bufferStates)
                .ForEach(
                    (Entity collectorEntity, int entityInQueryIndex, in Translation translation) =>
                    {
                        var position = translation.Value;
                        var rawSourceStates = new StateGroup(3, Allocator.Temp);

                        //准备出本collector附近的原料
                        for (var i = 0; i < bufferStates.Length; i++)
                        {
                            var state = bufferStates[i];
                            if (state.Trait != TypeManager.GetTypeIndex<RawSourceTrait>()) continue;
                            if (math.distance(state.Position, position) > CollectorRange) continue;
                            rawSourceStates.Add(state);
                        }

                        //写入collector
                        ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                        {
                            Target = collectorEntity,
                            Position = position,
                            Trait = TypeManager.GetTypeIndex<CollectorTrait>(),
                        });

                        //基于附近原料，写入潜在物品源
                        for (var i = 0; i < rawSourceStates.Length(); i++)
                        {
                            ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                            {
                                Target = collectorEntity,
                                Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                                ValueString = rawSourceStates[i].ValueString
                            });
                        }
                    }).Schedule(inputDeps);
            
            EcbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}