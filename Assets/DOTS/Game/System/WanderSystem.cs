using DOTS.Game.ComponentData;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DOTS.Game.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class WanderSystem : JobComponentSystem
    {
        public static Vector2 DistanceRange = new Vector2(2, 5);
        
        public EndSimulationEntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        [ExcludeComponent(typeof(Wandering))]
        private struct WanderStartJob : IJobForEachWithEntity<Wander, Translation>
        {
            public float Time;
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int index, ref Wander wander,
                ref Translation translation)
            {
                //在附近随机一个移动目标
                RandomTargetPosition(ECBuffer, entity, index, ref translation);
                
                //进入wandering状态
                ECBuffer.AddComponent(index, entity, new Wandering
                {
                    WanderStartTime = Time
                });
            }
        }
        
        /// <summary>
        /// 有Wandering而没有TargetPosition的entity表示走完了一段路程
        /// 如果还未到时，需要继续规划下一段，否则结束Wander
        /// </summary>
        [ExcludeComponent(typeof(TargetPosition))]
        private struct MoveDoneJob: IJobForEachWithEntity<Wander, Wandering, Translation>
        {
            public float Time;
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int index, ref Wander wander, ref Wandering wandering,
                ref Translation translation)
            {
                var startTime = wandering.WanderStartTime;
                if (Time - startTime < wander.Time)
                {
                    //继续下一个随机路点
                    RandomTargetPosition(ECBuffer, entity, index, ref translation);
                }
                else
                {
                    //结束
                    ECBuffer.RemoveComponent<Wandering>(index, entity);
                    ECBuffer.RemoveComponent<Wander>(index, entity);
                }
            }
        }
        
        private static void RandomTargetPosition(EntityCommandBuffer.Concurrent ecBuffer,
            Entity entity, int index, ref Translation translation)
        {
            var random = new Random();
            random.InitState();
            var direction = random.NextFloat2Direction();
            var distance = random.NextFloat(DistanceRange.x, DistanceRange.y);

            ecBuffer.AddComponent(index, entity, new TargetPosition
            {
                Value = translation.Value + new float3(distance * direction, 0)
            });
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var wanderStartJob = new WanderStartJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent(),
                Time = (float)Time.ElapsedTime
            };
            var moveDoneJob = new MoveDoneJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent(),
                Time = (float)Time.ElapsedTime
            };
            var wanderStartHandle = wanderStartJob.Schedule(this, inputDeps);
            var moveDoneHandle = moveDoneJob.Schedule(this, wanderStartHandle);
            ECBSystem.AddJobHandleForProducer(wanderStartHandle);
            ECBSystem.AddJobHandleForProducer(moveDoneHandle);
            return moveDoneHandle;
        }
    }
}