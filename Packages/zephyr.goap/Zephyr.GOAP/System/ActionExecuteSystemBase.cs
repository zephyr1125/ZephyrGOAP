using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;

namespace Zephyr.GOAP.System
{
    [UpdateInGroup(typeof(ActionExecuteSystemGroup))]
    public abstract class ActionExecuteSystemBase : JobComponentSystem
    {
        
        private EntityQuery _waitingActionNodeQuery;

        public EntityCommandBufferSystem EcbSystem;

        private FixedString32 _nameOfAction;

        protected override void OnCreate()
        {
            base.OnCreate();
            _nameOfAction = GetNameOfAction();
            _waitingActionNodeQuery = GetEntityQuery(new EntityQueryDesc{
                All =  new []{ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<ActionNodeActing>(), },
                None = new []{ComponentType.ReadOnly<NodeDependency>()}});
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //找出所有可执行的action
            var waitingNodeEntities = _waitingActionNodeQuery.ToEntityArray(Allocator.TempJob);
            var waitingNodes =
                _waitingActionNodeQuery.ToComponentDataArray<Node>(Allocator.TempJob);
            var waitingStates = GetBufferFromEntity<State>(true);

            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();

            var handle = ExecuteActionJob(_nameOfAction, waitingNodeEntities, waitingNodes,
                waitingStates, ecb, inputDeps);
            var handle2 = ExecuteActionJob2(_nameOfAction, waitingNodeEntities, waitingNodes,
                waitingStates, ecb, handle);
            EcbSystem.AddJobHandleForProducer(handle);
            if (!handle.Equals(handle2))
            {
                EcbSystem.AddJobHandleForProducer(handle2);
            }
            return handle2;
        }

        protected abstract JobHandle ExecuteActionJob(FixedString32 nameOfAction,
            NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, [ReadOnly]BufferFromEntity<State> waitingStates,
            EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps);

        /// <summary>
        /// 额外一个job，有的会需要
        /// </summary>
        /// <param name="nameOfAction"></param>
        /// <param name="waitingNodeEntities"></param>
        /// <param name="waitingNodes"></param>
        /// <param name="waitingStates"></param>
        /// <param name="ecb"></param>
        /// <param name="inputDeps"></param>
        /// <returns></returns>
        protected virtual JobHandle ExecuteActionJob2(FixedString32 nameOfAction,
            NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, [ReadOnly]BufferFromEntity<State> waitingStates,
            EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            return inputDeps;
        }

        protected abstract FixedString32 GetNameOfAction();
    }
}