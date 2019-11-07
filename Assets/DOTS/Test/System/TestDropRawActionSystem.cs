using System;
using DOTS.Component.Actions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace DOTS.Test.System
{
    public class TestDropRawActionSystem : JobComponentSystem, IDisposable
    {
        public StateGroup GoalStates;
        public StackData StackData;

        private EndInitializationEntityCommandBufferSystem _ECBSystem;

        protected override void OnCreate()
        {
            _ECBSystem =
                World.Active.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }
        
        [BurstCompile]
        private struct TestDropActionJob : IJobForEachWithEntity<DropRawAction>
        {
            [ReadOnly]
            public StateGroup GoalStates;
            [ReadOnly]
            public StackData StackData;
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity actionEntity, int jobIndex, [ReadOnly]ref DropRawAction action)
            {
                action.CreateNodes(ref GoalStates, ref StackData, jobIndex, ECBuffer);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new TestDropActionJob
            {
                GoalStates = GoalStates,
                StackData = StackData,
                ECBuffer = _ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var jobHandle = job.Schedule(this, inputDeps);
            _ECBSystem.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }

        public void Dispose()
        {
            GoalStates.Dispose();
            StackData.Dispose();
        }
    }
}