using Unity.Entities;

namespace Zephyr.GOAP.Tests.Mock
{
    [DisableAutoCreation]
    public class MockStateComponentSystem : SystemBase
    {
        public struct MockComponent : IComponentData
        {
            public double StartTime;
            public float DestroyPeriod;
            
        }

        public struct MockStateComponent : ISystemStateComponentData
        {
            public double StartTime;
            public float RemoveStatePeriod;
        }

        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var time = Time.ElapsedTime;
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            
            var watchHandle = Entities
                .WithAll<MockComponent>()
                .WithNone<MockStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex) =>
                {
                    ecb.AddComponent(entityInQueryIndex, entity, new MockStateComponent
                    {
                        StartTime = time,
                        RemoveStatePeriod = 10
                    });
                }).ScheduleParallel(Dependency);

            
            var destroyHandle = Entities
                .WithAll<MockStateComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, in MockComponent mockComponent) =>
                {
                    if (time - mockComponent.StartTime < mockComponent.DestroyPeriod) return;
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }).ScheduleParallel(watchHandle);
            
            var removeHandle = Entities
                .ForEach((Entity entity, int entityInQueryIndex, in MockStateComponent mockStateComponent) =>
                {
                    if (time - mockStateComponent.StartTime < mockStateComponent.RemoveStatePeriod) return;
                    ecb.RemoveComponent<MockStateComponent>(entityInQueryIndex, entity);
                }).ScheduleParallel(destroyHandle);
            
            EcbSystem.AddJobHandleForProducer(removeHandle);
        }
    }
}