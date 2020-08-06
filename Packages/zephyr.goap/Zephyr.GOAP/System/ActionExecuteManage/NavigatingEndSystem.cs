using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;

namespace Zephyr.GOAP.System.ActionExecuteManage
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NavigatingEndSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var handle = Entities.WithName("NavigatingEndJob")
                .WithAll<Agent>()
                .WithNone<TargetPosition>()
                .ForEach((Entity entity, int entityInQueryIndex, in Navigating navigating) =>
                {
                    //切换agent状态,可以进行Acting了
                    Utils.NextAgentState<Navigating, ReadyToAct>(entity, entityInQueryIndex,
                        ecb, navigating.NodeEntity);
                }).Schedule(inputDeps);
            EcbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}