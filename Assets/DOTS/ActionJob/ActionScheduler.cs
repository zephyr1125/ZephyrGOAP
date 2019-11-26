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
        
        //Output
        public NodeGraph NodeGraph;

        public NativeList<Node> NewlyExpandedNodes;

        public JobHandle Schedule(JobHandle inputDeps)
        {
            var dropRawHandle = new DropRawActionJob(ref UnexpandedNodes, ref StackData,
                ref NodeGraph, ref NewlyExpandedNodes).Schedule(
                UnexpandedNodes, 0, inputDeps);
            var pickRawHandle = new PickRawActionJob(ref UnexpandedNodes, ref StackData,
                ref NodeGraph, ref NewlyExpandedNodes).Schedule(
                UnexpandedNodes, 0, dropRawHandle);

            return pickRawHandle;
        }
    }
}