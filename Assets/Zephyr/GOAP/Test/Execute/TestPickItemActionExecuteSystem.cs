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
    public class TestPickItemActionExecuteSystem : TestActionExecuteBase
    {
        private PickItemActionExecuteSystem _system;
        private Entity _containerEntity, _actionNodeEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<PickItemActionExecuteSystem>();
            
            _containerEntity = EntityManager.CreateEntity();
            
            //container预先存好物品
            var itemBuffer = EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
            itemBuffer.Add(new ContainedItemRef
            {
                ItemName = new NativeString64("item"),
                ItemEntity = new Entity {Index = 99, Version = 9}
            });
            
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            
            //任务
            _actionNodeEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = new NativeString64(nameof(PickItemAction)),
                PreconditionsBitmask = 1,
                EffectsBitmask = 1 << 1,
            });
            EntityManager.AddComponentData(_actionNodeEntity, new ActionNodeActing());
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _containerEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = new NativeString64("item"),
            });
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemTransferTrait),
                ValueString = new NativeString64("item"),
            });
        }

        [Test]
        public void TargetRemoveItem()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(0, itemBuffer.Length);
        }

        [Test]
        public void AgentGotItem()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
            Assert.AreEqual(1, itemBuffer.Length);
            Assert.AreEqual(new ContainedItemRef
            {
                ItemName = new NativeString64("item"), ItemEntity = new Entity{Index = 99, Version = 9}
            }, itemBuffer[0]);
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