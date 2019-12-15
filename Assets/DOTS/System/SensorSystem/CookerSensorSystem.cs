using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System.SensorSystem
{
    /// <summary>
    /// 检测世界里的Cooker，写入其存在
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class CookerSensorSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBufferSystem;

        protected override void OnCreate()
        {
            ECBufferSystem = World.GetOrCreateSystem<SensorsSetCurrentStatesECBufferSystem>();
        }

        [RequireComponentTag(typeof(CookerTrait))]
        private struct SenseJob : IJobForEachWithEntity_EB<ContainedOutput>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;

            public Entity CurrentStatesEntity;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<ContainedOutput> recipes)
            {
                //写入cooker
                var buffer = ECBuffer.SetBuffer<State>(jobIndex, CurrentStatesEntity);
                buffer.Add(new State
                {
                    Target = entity,
                    Trait = typeof(CookerTrait),
                    IsPositive = true,
                });
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new SenseJob
            {
                ECBuffer = ECBufferSystem.CreateCommandBuffer().ToConcurrent(),
                CurrentStatesEntity = CurrentStatesHelper.CurrentStatesEntity
            };
            var handle = job.Schedule(this, inputDeps);
            ECBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}