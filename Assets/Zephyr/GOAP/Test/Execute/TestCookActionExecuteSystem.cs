using System.Linq;
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
    public class TestCookActionExecuteSystem : TestBase
    {
        private CookActionExecuteSystem _system;
        private Entity _agentEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<CookActionExecuteSystem>();

            _agentEntity = EntityManager.CreateEntity();
            
            //agent预存好原料
            var itemBuffer = EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            itemBuffer.Add(new ContainedItemRef
            {
                ItemName = new NativeString64("input0"),
                ItemEntity = new Entity {Index = 99, Version = 9}
            });
            itemBuffer.Add(new ContainedItemRef
            {
                ItemName = new NativeString64("input1"),
                ItemEntity = new Entity {Index = 98, Version = 9}
            });
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new ReadyToActing());
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            //agent必须带有已经规划好的任务列表
            var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
            bufferNodes.Add(new Node
            {
                Name = new NativeString64(nameof(CookAction)),
                PreconditionsBitmask = 3,
                EffectsBitmask = 1 << 2,
            });
            var bufferStates = EntityManager.AddBuffer<State>(_agentEntity);
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("input0"),
            });
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("input1"),
            });
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("output"),
            });
        }

        [Test]
        public void AgentRemoveInput()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
            var items = itemBuffer.ToNativeArray(Allocator.Temp);
            Assert.IsFalse(items.Any(item => item.ItemName.Equals(
                new NativeString64("input0"))));
            Assert.IsFalse(items.Any(item => item.ItemName.Equals(
                new NativeString64("input1"))));
            items.Dispose();
        }

        [Test]
        public void AgentGotOutput()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
            Assert.AreEqual(1, itemBuffer.Length);
            Assert.AreEqual(new NativeString64("output"), itemBuffer[0].ItemName);
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