using DOTS.Component;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.System
{
    /// <summary>
    /// 在Sensor运行前进行创建CurrentStates的工作
    /// 并且提供引用给其他System使用
    /// 在一帧的Simulation结束时回收
    /// </summary>
    [UpdateBefore(typeof(BeginInitializationEntityCommandBufferSystem))]
    public class CurrentStatesHelper : ComponentSystem
    {
        public static Entity CurrentStatesEntity;

        public EntityCommandBufferSystem _removeECBufferSystem;

        protected override void OnCreate()
        {
            _removeECBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            CurrentStatesEntity =
                EntityManager.CreateEntity(typeof(CurrentStates));
            EntityManager.AddBuffer<State>(CurrentStatesEntity);
            
            _removeECBufferSystem.CreateCommandBuffer().DestroyEntity(CurrentStatesEntity);
        }

        public static StateGroup GetCurrentStates(EntityManager entityManager, Allocator allocator)
        {
            var buffer = entityManager.GetBuffer<State>(CurrentStatesEntity);
            var states = new StateGroup(ref buffer, allocator);
            return states;
        }
    }
}