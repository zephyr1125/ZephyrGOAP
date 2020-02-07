using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NavigatingEndSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        // [BurstCompile]
        [RequireComponentTag(typeof(Navigating), typeof(Node))]
        [ExcludeComponent(typeof(TargetPosition))]
        private struct NavigatingEndJob : IJobForEachWithEntity<Agent>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, ref Agent agent)
            {
                //切换agent状态,可以进行Acting了
                Utils.NextAgentState<Navigating, ReadyToActing>(entity, jobIndex,
                    ref ECBuffer, agent, false);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new NavigatingEndJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var handle = job.Schedule(this, inputDeps);
            ECBSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}