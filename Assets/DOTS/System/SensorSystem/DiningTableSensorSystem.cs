using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Collections;
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
        public EntityCommandBufferSystem ECBufferSystem;
        private EntityQuery _diningTableQuery;

        protected override void OnCreate()
        {
            _diningTableQuery = GetEntityQuery(typeof(DiningTableTrait));
            ECBufferSystem = World.GetOrCreateSystem<SensorsSetCurrentStatesECBufferSystem>();
        }
        
        private struct SenseJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent ECBuffer;

            public Entity CurrentStatesEntity;

            [DeallocateOnJobCompletion]
            public NativeArray<Entity> Entities;

            public void Execute(int index)
            {
                //写入diningTable
                var buffer = ECBuffer.AddBuffer<State>(index, CurrentStatesEntity);
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
                ECBuffer = ECBufferSystem.CreateCommandBuffer().ToConcurrent(),
                CurrentStatesEntity = CurrentStatesHelper.CurrentStatesEntity,
                Entities = entities
            };
            var handle = job.Schedule(entities.Length, 32, inputDeps);
            ECBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}