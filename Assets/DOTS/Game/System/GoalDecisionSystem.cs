using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace DOTS.Game.System
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

        [BurstCompile]
        [RequireComponentTag(typeof(NoGoal))]
        private struct GoalDecisionJob : IJobForEachWithEntity_EBBCC<Node, State, Agent, Stamina>
        {
            public float MinStamina;
            public float MaxStamina;

            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent, ref Stamina stamina)
            {
                Assert.AreEqual(0, agent.ExecutingNodeId);
                Assert.AreEqual(0, nodes.Length);
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
                        IsPositive = true
                    });
                }
                else
                {
                    //wander
                    states.Add(new State
                    {
                        Target = entity,
                        Trait = typeof(WanderTrait),
                        IsPositive = true
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