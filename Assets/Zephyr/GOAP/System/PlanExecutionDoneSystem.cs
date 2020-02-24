using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System
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
        [RequireComponentTag(typeof(ReadyToNavigate))]
        private struct PlanExecutionDoneJob : IJobForEachWithEntity_EBBCC<Node, State, Agent, CurrentGoal>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent, [ReadOnly]ref CurrentGoal currentGoal)
            {
                var pathLength = nodes.Length;
                if (agent.ExecutingNodeId < pathLength) return;
                
                ECBuffer.RemoveComponent<Node>(jobIndex, entity);
                states.Clear();
                agent.ExecutingNodeId = 0;
                
                ECBuffer.DestroyEntity(jobIndex, currentGoal.GoalEntity);
                ECBuffer.RemoveComponent<CurrentGoal>(jobIndex, entity);
                
                Utils.NextAgentState<ReadyToNavigate, NoGoal>(entity, jobIndex, ref ECBuffer,
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