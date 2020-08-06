using Unity.Entities;
using Unity.Jobs;

namespace Zephyr.GOAP.System
{
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public abstract class SensorSystemBase : JobComponentSystem
    {
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

            var handle = ScheduleSensorJob(inputDeps, ecb, baseStateEntity);
            EcbSystem.AddJobHandleForProducer(handle);
            return handle;
        }

        protected abstract JobHandle ScheduleSensorJob(JobHandle inputDeps,
            EntityCommandBuffer.ParallelWriter ecb, Entity baseStateEntity);
    }
}