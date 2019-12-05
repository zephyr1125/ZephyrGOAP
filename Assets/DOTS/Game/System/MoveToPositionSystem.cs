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
        [BurstCompile]
        public struct MoveToPositionJob : IJobForEach<Translation, MaxMoveSpeed, TargetPosition>
        {
            public float deltaSecond;
            
            public void Execute(ref Translation position, [ReadOnly]ref MaxMoveSpeed maxMoveSpeed,
                ref TargetPosition targetPosition)
            {
                var targetPos = targetPosition.Value;
                if (targetPos.Equals(float3.zero)) return;
                
                var speed = maxMoveSpeed.value;
                var pos = position.Value;
                var newPosition = Vector3.MoveTowards(pos, targetPos,
                    speed * deltaSecond);
                position.Value = newPosition;
                
                //到达后重置target
                if (newPosition.Equals(targetPos))
                {
                    targetPosition.Value = float3.zero;
                }
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new MoveToPositionJob
            {
                deltaSecond = GameTime.Instance().DeltaSecond
            };
            return job.Schedule(this, inputDeps);
        }
    }
}