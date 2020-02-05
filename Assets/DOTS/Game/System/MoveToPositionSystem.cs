using DOTS.Game.ComponentData;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTS.Game.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MoveToPositionSystem : JobComponentSystem
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        // [BurstCompile]
        public struct MoveToPositionJob : IJobForEachWithEntity<Translation, MaxMoveSpeed, TargetPosition>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public float deltaSecond;
            
            public void Execute(Entity entity, int jobIndex, ref Translation position,
                [ReadOnly]ref MaxMoveSpeed maxMoveSpeed, ref TargetPosition targetPosition)
            {
                var targetPos = targetPosition.Value;
                
                var speed = maxMoveSpeed.value;
                var pos = position.Value;
                var newPosition = Vector3.MoveTowards(pos, targetPos,
                    speed * deltaSecond);
                position.Value = newPosition;
                
                //到达后移除target
                if (newPosition.Equals(targetPos))
                {
                    ECBuffer.RemoveComponent<TargetPosition>(jobIndex, entity);
                }
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new MoveToPositionJob
            {
                deltaSecond = Time.DeltaTime,
                ECBuffer = _ecbSystem.CreateCommandBuffer().ToConcurrent()
            };
            var handle = job.Schedule(this, inputDeps);
            _ecbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}