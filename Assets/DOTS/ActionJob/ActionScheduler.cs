using System.Collections.Generic;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.ActionJob
{
    public class ActionScheduler
    {
        // Input
        [ReadOnly]
        public NativeList<Node> UnexpandedNodes;

        [ReadOnly]
        public StackData StackData;
        
        public NodeGraphGroup NodeGraphGroup;

        public JobHandle Schedule(JobHandle inputDeps)
        {
            var allActionJobHandles = new NativeArray<JobHandle>(1, Allocator.TempJob)
            {
                [0] = new DropRawActionJob(UnexpandedNodes, StackData, NodeGraphGroup).Schedule(
                    UnexpandedNodes, 0, inputDeps),
            };

            var combinedHandle = JobHandle.CombineDependencies(allActionJobHandles);
            
            allActionJobHandles.Dispose();

            return combinedHandle;
        }
    }
}