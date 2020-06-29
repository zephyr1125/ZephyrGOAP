using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExecute
{
    public class TestDropRawActionExecuteSystem : TestActionExecuteBase
    {
        private DropRawActionExecuteSystem _system;

        private Entity _containerEntity; 

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<DropRawActionExecuteSystem>();
            _containerEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = new Entity {Index = 9, Version = 9},
                ItemName = "item"
            });
            
            buffer = EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = new Entity {Index = 8, Version = 9},
                ItemName = "origin"
            });
            
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(DropRawAction),
                PreconditionsBitmask = 1,
                EffectsBitmask = 1 << 1
            });
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(RawTransferTrait),
                ValueString = "item",
            });
            bufferStates.Add(new State
            {
                Target = _containerEntity,
                Trait = typeof(RawDestinationTrait),
                ValueString = "item",
            });
        }

        [Test]
        public void TargetGotItem()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(2, itemBuffer.Length);
            Assert.AreEqual(new ContainedItemRef
            {
                ItemEntity = new Entity {Index = 8, Version = 9},
                ItemName = "origin"
            }, itemBuffer[0]);
            Assert.AreEqual(new ContainedItemRef
            {
                ItemEntity = new Entity {Index = 9, Version = 9},
                ItemName = "item"
            }, itemBuffer[1]);
        }
    }
}