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
    public class TestDropItemActionExecuteSystem : TestActionExecuteBase
    {
        private DropItemActionExecuteSystem _system;

        private Entity _containerEntity, _itemEntity; 

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<DropItemActionExecuteSystem>();
            _containerEntity = EntityManager.CreateEntity();
            _itemEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_itemEntity, new Item());
            EntityManager.AddComponentData(_itemEntity, new Name{Value = "item"});
            EntityManager.AddComponentData(_itemEntity, new Count{Value = 1});

            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = _itemEntity,
                ItemName = "item"
            });
            
            EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
            
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(DropItemAction),
                PreconditionsBitmask = 1,
                EffectsBitmask = 1 << 1
            });
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<ItemTransferTrait>(),
                ValueString = "item",
                Amount = 1
            });
            bufferStates.Add(new State
            {
                Target = _containerEntity,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString ="item",
                Amount = 1
            });
        }

        [Test]
        public void TargetGotItem()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(1, itemBuffer.Length);
            var itemEntity = itemBuffer[0].ItemEntity;
            Assert.AreEqual("item", EntityManager.GetComponentData<Name>(itemEntity).Value);    
            Assert.AreEqual(1, EntityManager.GetComponentData<Count>(itemEntity).Value);
        }
    }
}