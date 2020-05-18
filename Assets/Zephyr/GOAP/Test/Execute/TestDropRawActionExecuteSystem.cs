using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System.ActionExecuteSystem;

namespace Zephyr.GOAP.Test.Execute
{
    public class TestDropRawActionExecuteSystem : TestActionExecuteBase
    {
        private DropRawActionExecuteSystem _system;

        private Entity _actionNodeEntity, _containerEntity; 

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

            _actionNodeEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(DropRawAction),
                PreconditionsBitmask = 1,
                EffectsBitmask = 1 << 1
            });
            EntityManager.AddComponentData(_actionNodeEntity, new ActionNodeActing());
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(RawTransferTrait),
                ValueString = new NativeString64("item"),
            });
            bufferStates.Add(new State
            {
                Target = _containerEntity,
                Trait = typeof(RawDestinationTrait),
                ValueString = new NativeString64("item"),
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
                ItemName = new NativeString64("origin")
            }, itemBuffer[0]);
            Assert.AreEqual(new ContainedItemRef
            {
                ItemEntity = new Entity {Index = 9, Version = 9},
                ItemName = new NativeString64("item")
            }, itemBuffer[1]);
        }

        [Test]
        public void ProgressGoOn()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.False(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
            Assert.True(EntityManager.HasComponent<ActDone>(_agentEntity));
            
            Assert.False(EntityManager.HasComponent<ActionNodeActing>(_actionNodeEntity));
            Assert.True(EntityManager.HasComponent<ActionNodeDone>(_actionNodeEntity));
        }
    }
}