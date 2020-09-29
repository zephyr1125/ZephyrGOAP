using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Zephyr.GOAP.Component;

namespace Zephyr.GOAP.Sample.Game.System
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
        public struct MoveToPositionJob : IJobForEachWithEntity<Translation, AgentMoveSpeed, TargetPosition>
        {
            public EntityCommandBuffer.ParallelWriter ECBuffer;
            
            public float deltaSecond;
            
            public void Execute(Entity entity, int jobIndex, ref Translation position,
                [ReadOnly]ref AgentMoveSpeed agentMoveSpeed, ref TargetPosition targetPosition)
            {
                var targetPos = targetPosition.Value;
                
                var speed = agentMoveSpeed.value;
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
                ECBuffer = _ecbSystem.CreateCommandBuffer().AsParallelWriter()
            };
            var handle = job.Schedule(this, inputDeps);
            _ecbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}