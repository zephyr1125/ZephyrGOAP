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
        public NativeList<Entity> UnexpandedNodes;

        [ReadOnly]
        public StackData StackData;

        public EntityCommandBufferSystem ECBufferSystem;

        private ActionScheduler _actionScheduler;

        protected override void OnCreate()
        {
            ECBufferSystem =
                World.Active.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            _actionScheduler = new ActionScheduler
            {
                UnexpandedNodes = UnexpandedNodes,
                BuffersState = GetBufferFromEntity<State>(true),
                StackData = StackData,
                ECBufferSystem = ECBufferSystem
            };
            return _actionScheduler.Schedule(inputDeps);
        }
    }
}
#endif