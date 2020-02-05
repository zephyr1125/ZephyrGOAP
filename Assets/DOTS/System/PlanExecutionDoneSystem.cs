using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Struct;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System
{
    
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class PlanExecutionDoneSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        // [BurstCompile]
        [RequireComponentTag(typeof(ReadyToNavigating))]
        private struct PlanExecutionDoneJob : IJobForEachWithEntity_EBBC<Node, State, Agent>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent)
            {
                var pathLength = nodes.Length;
                if (agent.ExecutingNodeId < pathLength) return;
                
                ECBuffer.RemoveComponent<Node>(jobIndex, entity);
                states.Clear();
                agent.ExecutingNodeId = 0;
                
                Utils.NextAgentState<ReadyToNavigating, NoGoal>(entity, jobIndex, ref ECBuffer,
                    agent, false);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new PlanExecutionDoneJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var handle = job.Schedule(this, inputDeps);
            ECBSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}