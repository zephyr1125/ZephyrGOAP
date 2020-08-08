using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System.SensorManage;

namespace Zephyr.GOAP.System
{
    /// <summary>
    /// 在Sensor运行前进行创建BaseStates的工作
    /// 并且提供引用给其他System使用
    /// 在一帧的Simulation结束时回收
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(SensorSystemGroup))]
    public class BaseStatesHelper : ComponentSystem
    {
        public static Entity BaseStatesEntity;

        public EntityCommandBufferSystem _removeECBufferSystem;

        protected override void OnCreate()
        {
            _removeECBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            BaseStatesEntity = EntityManager.CreateEntity(typeof(BaseStates), typeof(State));
        }

        protected override void OnUpdate()
        {
            var buffer = _removeECBufferSystem.CreateCommandBuffer()
                .SetBuffer<State>(BaseStatesEntity);
            buffer.Clear();
        }

        public static StateGroup GetBaseStates(EntityManager entityManager, Allocator allocator)
        {
            var buffer = entityManager.GetBuffer<State>(BaseStatesEntity);
            var states = new StateGroup(buffer, allocator);
            return states;
        }
    }
}