using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Game.System;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Test.Game
{
    public class TestGoalDecisionSystem : TestBase
    {
        private GoalDecisionSystem _system;
        private Entity _agentEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.GetOrCreateSystem<GoalDecisionSystem>();
            _system.MaxStamina = 0.5f;
            _system.MinStamina = 0.5f;
            _agentEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new NoGoal());
            EntityManager.AddComponentData(_agentEntity, new Stamina{Value = 1});
            EntityManager.AddBuffer<Node>(_agentEntity);
            EntityManager.AddBuffer<State>(_agentEntity);
        }

        [Test]
        public void Low_AddStamina()
        {
            EntityManager.SetComponentData(_agentEntity, new Stamina{Value = 0.4f});
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var stateBuffer = EntityManager.GetBuffer<State>(_agentEntity);
            Assert.AreEqual(1, stateBuffer.Length);
            Assert.AreEqual(new State
            {
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            }, stateBuffer[0]);
        }

        [Test]
        public void High_Wander()
        {
            EntityManager.SetComponentData(_agentEntity, new Stamina{Value = 0.8f});
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var stateBuffer = EntityManager.GetBuffer<State>(_agentEntity);
            Assert.AreEqual(1, stateBuffer.Length);
            Assert.AreEqual(new State
            {
                Target = _agentEntity,
                Trait = typeof(WanderTrait),
            }, stateBuffer[0]);
            
        }

        [Test]
        public void NextAgentState()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<GoalPlanning>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<NoGoal>(_agentEntity));
        }
    }
}