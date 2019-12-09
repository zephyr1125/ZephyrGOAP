using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace DOTS.System
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
        
        [RequireComponentTag(typeof(Navigating), typeof(Node))]
        private struct NavigatingEndJob : IJobForEachWithEntity<Agent, TargetPosition>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, ref Agent agent, ref TargetPosition targetPosition)
            {
                //移动结束后会重置TargetPosition为0，在此之前都等待其结束
                if(!targetPosition.Value.Equals(float3.zero)) return;
                
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