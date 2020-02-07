using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.SensorSystem
{
    /// <summary>
    /// 检测世界里的DiningTable，写入其存在
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class DiningTableSensorSystem : JobComponentSystem
    {
        private EntityQuery _diningTableQuery;

        protected override void OnCreate()
        {
            _diningTableQuery = GetEntityQuery(typeof(DiningTableTrait), typeof(Translation));
        }
        
        private struct SenseJob : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction, WriteOnly]
            public BufferFromEntity<State> States;

            public Entity CurrentStatesEntity;

            [DeallocateOnJobCompletion]
            public NativeArray<Entity> Entities;

            [DeallocateOnJobCompletion]
            public NativeArray<Translation> Translations;

            public void Execute(int index)
            {
                //写入diningTable
                var buffer = States[CurrentStatesEntity];
                buffer.Add(new State
                {
                    Target = Entities[index],
                    Position = Translations[index].Value,
                    Trait = typeof(DiningTableTrait),
                });
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entities = _diningTableQuery.ToEntityArray(Allocator.TempJob);
            var translations =
                _diningTableQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
            
            var job = new SenseJob
            {
                States = GetBufferFromEntity<State>(),
                CurrentStatesEntity = CurrentStatesHelper.CurrentStatesEntity,
                Entities = entities,
                Translations = translations
            };
            var handle = job.Schedule(entities.Length, 32, inputDeps);
            return handle;
        }
    }
}