using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System.SensorSystem
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
            _diningTableQuery = GetEntityQuery(typeof(DiningTableTrait));
        }
        
        private struct SenseJob : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction, WriteOnly]
            public BufferFromEntity<State> States;

            public Entity CurrentStatesEntity;

            [DeallocateOnJobCompletion]
            public NativeArray<Entity> Entities;

            public void Execute(int index)
            {
                //写入diningTable
                var buffer = States[CurrentStatesEntity];
                buffer.Add(new State
                {
                    Target = Entities[index],
                    Trait = typeof(DiningTableTrait),
                });
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entities = _diningTableQuery.ToEntityArray(Allocator.TempJob);
            
            var job = new SenseJob
            {
                States = GetBufferFromEntity<State>(),
                CurrentStatesEntity = CurrentStatesHelper.CurrentStatesEntity,
                Entities = entities
            };
            var handle = job.Schedule(entities.Length, 32, inputDeps);
            return handle;
        }
    }
}