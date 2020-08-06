using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Random = Unity.Mathematics.Random;

namespace Zephyr.GOAP.Sample.Game.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class WanderSystem : JobComponentSystem
    {
        public static Vector2 DistanceRange = new Vector2(4, 6);
        
        public EndSimulationEntityCommandBufferSystem ECBSystem;

        private Random _random;

        public int MaxRandomResults = 99;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _random = new Random((uint) DateTime.Now.Millisecond);
        }

        // [BurstCompile]
        [ExcludeComponent(typeof(Wandering))]
        private struct WanderStartJob : IJobForEachWithEntity<Wander, Translation>
        {
            public float Time;
            public NativeArray<float2> RandomDirections;
            public NativeArray<float> RandomDistances;
            public EntityCommandBuffer.ParallelWriter ECBuffer;
            
            public void Execute(Entity entity, int index, ref Wander wander,
                ref Translation translation)
            {
                //在附近随机一个移动目标
                RandomTargetPosition(RandomDirections, RandomDistances, index,
                    ECBuffer, entity, index, translation);
                
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
        // [BurstCompile]
        [ExcludeComponent(typeof(TargetPosition))]
        private struct MoveDoneJob: IJobForEachWithEntity<Wander, Wandering, Translation>
        {
            public float Time;
            [DeallocateOnJobCompletion]
            public NativeArray<float2> RandomDirections;
            [DeallocateOnJobCompletion]
            public NativeArray<float> RandomDistances;
            public EntityCommandBuffer.ParallelWriter ECBuffer;
            
            public void Execute(Entity entity, int index, ref Wander wander, ref Wandering wandering,
                ref Translation translation)
            {
                var startTime = wandering.WanderStartTime;
                if (Time - startTime < wander.Time)
                {
                    //继续下一个随机路点
                    RandomTargetPosition(RandomDirections, RandomDistances, index,
                        ECBuffer, entity, index, translation);
                }
                else
                {
                    //结束
                    ECBuffer.RemoveComponent<Wandering>(index, entity);
                    ECBuffer.RemoveComponent<Wander>(index, entity);
                }
            }
        }
        
        private static void RandomTargetPosition(NativeArray<float2> randomDirections,
            NativeArray<float> randomDistances, int jobId, EntityCommandBuffer.ParallelWriter ecBuffer,
            Entity entity, int index, Translation translation)
        {
            var direction2 = randomDirections[jobId];
            var direction3 = new float3(direction2.x, 0, direction2.y);    //只在水平面上的随机
            var distance = randomDistances[jobId];

            ecBuffer.AddComponent(index, entity, new TargetPosition
            {
                Value = translation.Value + new float3(distance * direction3)
            });
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var randomDirections = new NativeArray<float2>(MaxRandomResults, Allocator.TempJob);
            var randomDistances = new NativeArray<float>(MaxRandomResults, Allocator.TempJob);
            for (var i = 0; i < MaxRandomResults; i++)
            {
                randomDirections[i] = _random.NextFloat2Direction();
                randomDistances[i] = _random.NextFloat();
            }
            
            var wanderStartJob = new WanderStartJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().AsParallelWriter(),
                RandomDirections = randomDirections,
                RandomDistances = randomDistances,
                Time = (float)Time.ElapsedTime
            };
            var moveDoneJob = new MoveDoneJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().AsParallelWriter(),
                RandomDirections = randomDirections,
                RandomDistances = randomDistances,
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