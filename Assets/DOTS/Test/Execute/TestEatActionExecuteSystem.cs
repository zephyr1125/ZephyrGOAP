using System.Linq;
using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using DOTS.System.ActionExecuteSystem;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Test.Execute
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
                ItemName = new NativeString64("food"),
                ItemEntity = new Entity {Index = 99, Version = 9}
            });
            EntityManager.AddComponentData(_agentEntity, new Stamina {Value = 0});
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new ReadyToActing());
            EntityManager.AddComponentData(_agentEntity, new EatAction());
            //agent必须带有已经规划好的任务列表
            var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
            bufferNodes.Add(new Node
            {
                Name = new NativeString64(nameof(EatAction)),
                PreconditionsBitmask = 1 << 0,
                EffectsBitmask = 1 << 1,
            });
            var bufferStates = EntityManager.AddBuffer<State>(_agentEntity);
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("food"),
            });
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
            Assert.False(EntityManager.HasComponent<ReadyToActing>(_agentEntity));
            Assert.True(EntityManager.HasComponent<ReadyToNavigating>(_agentEntity));
        }
    }
}