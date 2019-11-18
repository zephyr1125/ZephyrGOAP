
using DOTS.Struct;
#if UNITY_EDITOR
using DOTS.ActionJob;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.Test.System
{
    public class TestActionSchedulerSystem : JobComponentSystem
    {
        [ReadOnly]
        public NativeList<Node> UnexpandedNodes;

        [ReadOnly]
        public StackData StackData;

        public NodeGraph NodeGraph;


        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var actionScheduler = new ActionScheduler
            {
                UnexpandedNodes = UnexpandedNodes,
                StackData = StackData,
                NodeGraph = NodeGraph
            };
            return actionScheduler.Schedule(inputDeps);
        }
    }
}
#endif