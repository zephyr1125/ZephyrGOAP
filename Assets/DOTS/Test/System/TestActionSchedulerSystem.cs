
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

        public NodeGraphGroup NodeGraphGroup;

        private ActionScheduler _actionScheduler;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            _actionScheduler = new ActionScheduler
            {
                UnexpandedNodes = UnexpandedNodes,
                StackData = StackData,
                NodeGraphGroup = NodeGraphGroup
            };
            return _actionScheduler.Schedule(inputDeps);
        }
    }
}
#endif