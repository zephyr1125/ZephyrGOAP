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
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(SensorSystemGroup))]
    public class CurrentStatesHelper : ComponentSystem
    {
        public static Entity CurrentStatesEntity;

        public EntityCommandBufferSystem _removeECBufferSystem;

        protected override void OnCreate()
        {
            _removeECBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            CurrentStatesEntity = EntityManager.CreateEntity(typeof(CurrentStates), typeof(State));
        }

        protected override void OnUpdate()
        {
            var buffer = _removeECBufferSystem.CreateCommandBuffer()
                .SetBuffer<State>(CurrentStatesEntity);
            buffer.Clear();
        }

        public static StateGroup GetCurrentStates(EntityManager entityManager, Allocator allocator)
        {
            var buffer = entityManager.GetBuffer<State>(CurrentStatesEntity);
            var states = new StateGroup(ref buffer, allocator);
            return states;
        }
    }
}