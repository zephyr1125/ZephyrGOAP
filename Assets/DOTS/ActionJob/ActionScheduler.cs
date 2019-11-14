using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.ActionJob
{
    public class ActionScheduler
    {
        // Input
        public NativeList<Entity> UnexpandedNodes;

        [ReadOnly]
        public BufferFromEntity<State> BuffersState;

        [ReadOnly]
        public StackData StackData;
        
        public EntityCommandBufferSystem ECBufferSystem;

        public JobHandle Schedule(JobHandle inputDeps)
        {
            var allActionJobHandles = new NativeArray<JobHandle>(1, Allocator.TempJob)
            {
                [0] = new DropRawActionJob(UnexpandedNodes, BuffersState, StackData,
                    ECBufferSystem.CreateCommandBuffer().ToConcurrent()).Schedule(
                    UnexpandedNodes, 0, inputDeps),
            };
            foreach (var handle in allActionJobHandles)
            {
                ECBufferSystem.AddJobHandleForProducer(handle);
            }

            var combinedHandle = JobHandle.CombineDependencies(allActionJobHandles);
            
            allActionJobHandles.Dispose();

            return combinedHandle;
        }
    }
}