using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        
        [RequireComponentTag(typeof(CollectorTrait))]
        private struct SenseJob : IJobForEachWithEntity_EC<Translation>
        {
            public float CollectorRange;
            
            [NativeDisableContainerSafetyRestriction]
            public BufferFromEntity<State> States;

            public Entity BaseStatesEntity;
            
            public void Execute(Entity entity, int jobIndex, ref Translation translation)
            {
                var buffer = States[BaseStatesEntity];
                var position = translation.Value;
                var rawSourceStates = new StateGroup(3, Allocator.Temp);
                
                //准备出本collector附近的原料
                for (var i = 0; i < buffer.Length; i++)
                {
                    var state = buffer[i];
                    if (state.Trait != TypeManager.GetTypeIndex<RawSourceTrait>()) continue;
                    if (math.distance(state.Position, position) > CollectorRange) continue;
                    rawSourceStates.Add(state);
                }

                //写入collector
                buffer.Add(new State
                {
                    Target = entity,
                    Position = position,
                    Trait = TypeManager.GetTypeIndex<CollectorTrait>(),
                });
                //基于附近原料，写入潜在物品源
                for (var i = 0; i < rawSourceStates.Length(); i++)
                {
                    buffer.Add(new State
                    {
                        Target = entity,
                        Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                        ValueString = rawSourceStates[i].ValueString
                    });
                }
                
                rawSourceStates.Dispose();
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new SenseJob
            {
                CollectorRange = CollectorRange,
                States = GetBufferFromEntity<State>(),
                BaseStatesEntity = BaseStatesHelper.BaseStatesEntity
            };
            var handle = job.Schedule(this, inputDeps);
            return handle;
        }
    }
}