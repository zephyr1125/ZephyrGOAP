using DOTS.Component;
using DOTS.Component.Actions;
using DOTS.Component.Trait;
using DOTS.Test.System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Runtime.Component;

namespace DOTS.Test
{
    public class TestDropRawAction : TestBase
    {
        private Entity _containerEntity, _agentEntity;
        
        private TestDropRawActionSystem _system;
        private EndInitializationEntityCommandBufferSystem _ECBSystem;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.GetOrCreateSystem<TestDropRawActionSystem>();
            _ECBSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();

            _containerEntity = EntityManager.CreateEntity();
            _agentEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());


            //goal: 指定容器具有指定物品
            _system.GoalStates = new StateGroup(1, Allocator.Persistent)
            {
                new State
                {
                    Target = _containerEntity,
                    Trait = typeof(Inventory),
                    StringValue = new NativeString64("test"),
                }
            };

            //setting: 自身具有指定物品
            _system.StackData = new StackData
            {
                AgentEntity = _agentEntity,
                Settings = new StateGroup(1, Allocator.Persistent)
                {
                    new State
                    {
                        Target = _agentEntity,
                        Trait = typeof(Inventory),
                        StringValue = new NativeString64("test")
                    }
                }
            };
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _system.Dispose();
        }

        [Test]
        public void TestRunCreateNode()
        {
            _system.Update();
            _ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var nodeQuery = EntityManager.CreateEntityQuery(typeof(Node));
            var nodeEntities = nodeQuery.ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(1, nodeEntities.Length);
            var buffer = EntityManager.GetBuffer<State>(nodeEntities[0]);
            var state = buffer[0];
            Assert.AreEqual(new State
            {
                Target = _agentEntity,
                Trait = typeof(Inventory),
                StringValue = new NativeString64("test")
            }, state);
            nodeEntities.Dispose();
        }
    }
}