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
    public class TestEatActionExecuteSystem : TestBase
    {
        private EatActionExecuteSystem _system;
        private Entity _agentEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<EatActionExecuteSystem>();

            _agentEntity = EntityManager.CreateEntity();
            
            //agent预存好食物
            var itemBuffer = EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            itemBuffer.Add(new ContainedItemRef
            {
                ItemName = new NativeString64("roast_apple"),
                ItemEntity = new Entity {Index = 99, Version = 9}
            });
            EntityManager.AddComponentData(_agentEntity, new Stamina {Value = 0});
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new ReadyToAct());
            EntityManager.AddComponentData(_agentEntity, new EatAction());
            //agent必须带有已经规划好的任务列表
            var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
            bufferNodes.Add(new Node
            {
                Name = new NativeString64(nameof(EatAction)),
                PreconditionsBitmask = (1<<0) + (1<<1),
                EffectsBitmask = 1 << 2,
            });
            var bufferStates = EntityManager.AddBuffer<State>(_agentEntity);
            //preconditions
            bufferStates.Add(new State
            {
                Trait = typeof(DiningTableTrait),
            });
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
                ValueString = new NativeString64("roast_apple"),
            });
            //effect
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            });
        }

        [Test]
        public void AgentRemoveFood()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
            Assert.Zero(itemBuffer.Length);
        }

        [Test]
        public void AgentGotStamina()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(0.5f, EntityManager.GetComponentData<Stamina>(_agentEntity).Value);
        }

        [Test]
        public void ProgressGoOn()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var agent = EntityManager.GetComponentData<Agent>(_agentEntity);
            Assert.AreEqual(1,agent.ExecutingNodeId);
            Assert.False(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
            Assert.True(EntityManager.HasComponent<ReadyToNavigate>(_agentEntity));
        }
    }
}