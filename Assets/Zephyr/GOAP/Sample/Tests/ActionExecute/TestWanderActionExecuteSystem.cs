using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExecute
{
    public class TestWanderActionExecuteSystem : TestActionExecuteBase
    {
        private WanderActionExecuteSystem _system;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<WanderActionExecuteSystem>();

            EntityManager.AddComponentData(_agentEntity, new WanderAction());
            
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(WanderAction),
                PreconditionsBitmask = 0,
                EffectsBitmask = 1,
            });
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<WanderTrait>(),
            });
        }

        [Test]
        public void AgentStartWander()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<Wander>(_agentEntity));
            Assert.IsTrue(EntityManager.HasComponent<Acting>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
        }

        [Test]
        public void ProgressGoOnAfterWander()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            EntityManager.RemoveComponent<Wander>(_agentEntity);
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.False(EntityManager.HasComponent<Acting>(_agentEntity));
            Assert.True(EntityManager.HasComponent<ActDone>(_agentEntity));
            
            Assert.False(EntityManager.HasComponent<ActionNodeActing>(_actionNodeEntity));
            Assert.True(EntityManager.HasComponent<ActionNodeDone>(_actionNodeEntity));
        }
    }
}