using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System.ActionExecuteSystem;

namespace Zephyr.GOAP.Test.Execute
{
    public class TestWanderActionExecuteSystem : TestBase
    {
        private WanderActionExecuteSystem _system;
        private Entity _agentEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<WanderActionExecuteSystem>();

            _agentEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new ReadyToActing());
            EntityManager.AddComponentData(_agentEntity, new WanderAction());
            //agent必须带有已经规划好的任务列表
            var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
            bufferNodes.Add(new Node
            {
                Name = new NativeString64(nameof(WanderAction)),
                PreconditionsBitmask = 0,
                EffectsBitmask = 1,
            });
            var bufferStates = EntityManager.AddBuffer<State>(_agentEntity);
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(WanderTrait),
            });
        }

        [Test]
        public void AgentStartWander()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<Wander>(_agentEntity));
            Assert.IsTrue(EntityManager.HasComponent<Acting>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToActing>(_agentEntity));
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }

        [Test]
        public void ProgressGoOnAfterWander()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            EntityManager.RemoveComponent<Wander>(_agentEntity);
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var agent = EntityManager.GetComponentData<Agent>(_agentEntity);
            Assert.AreEqual(1,agent.ExecutingNodeId);
            Assert.False(EntityManager.HasComponent<ReadyToActing>(_agentEntity));
            Assert.True(EntityManager.HasComponent<ReadyToNavigating>(_agentEntity));
        }
    }
}