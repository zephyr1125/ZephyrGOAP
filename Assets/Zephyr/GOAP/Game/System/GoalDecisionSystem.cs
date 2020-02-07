using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Game.System
{
    /// <summary>
    /// Goal Decision 示例
    /// Stamina低于EatThreshold则AddStamina否则Wandering
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class GoalDecisionSystem : JobComponentSystem
    {
        public float MinStamina = 0.2f;
        public float MaxStamina = 0.8f;

        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        // [BurstCompile]
        [RequireComponentTag(typeof(NoGoal))]
        private struct GoalDecisionJob : IJobForEachWithEntity_EBCC<State, Agent, Stamina>
        {
            public float MinStamina;
            public float MaxStamina;

            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex,
                DynamicBuffer<State> states, ref Agent agent, ref Stamina stamina)
            {
                Assert.AreEqual(0, agent.ExecutingNodeId);
                Assert.AreEqual(0, states.Length);

                var random = new Random();
                random.InitState();
                var eatThreshold = random.NextFloat(MinStamina, MaxStamina);

                if (stamina.Value < eatThreshold)
                {
                    //need more stamina
                    states.Add(new State
                    {
                        Target = entity,
                        Trait = typeof(StaminaTrait),
                    });
                }
                else
                {
                    //wander
                    states.Add(new State
                    {
                        Target = entity,
                        Trait = typeof(WanderTrait),
                    });
                }
                
                //ready to plan
                Utils.NextAgentState<NoGoal, GoalPlanning>(entity, jobIndex, ref ECBuffer, agent,
                    false);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new GoalDecisionJob
            {
                MaxStamina = MaxStamina,
                MinStamina = MinStamina,
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var handle = job.Schedule(this, inputDeps);
            ECBSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}