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

        private Entity _containerEntity, _itemEntity; 

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<DropRawActionExecuteSystem>();
            _containerEntity = EntityManager.CreateEntity();
            _itemEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = _itemEntity,
                ItemName = "item"
            });

            EntityManager.AddComponentData(_itemEntity, new Item());
            EntityManager.AddComponentData(_itemEntity, new Name{Value = "item"});
            EntityManager.AddComponentData(_itemEntity, new Count{Value = 1});
            
            EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
            
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
                Trait = TypeManager.GetTypeIndex<RawTransferTrait>(),
                ValueString = "item",
                Amount = 1
            });
            bufferStates.Add(new State
            {
                Target = _containerEntity,
                Trait = TypeManager.GetTypeIndex<RawDestinationTrait>(),
                ValueString = "item",
                Amount = 1
            });
        }

        [Test]
        public void TargetGotItem()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            //agent的物品减少数量
            Assert.Zero(EntityManager.GetComponentData<Count>(_itemEntity).Value);
            
            //目标容器增加物品
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(1, itemBuffer.Length);
            var newItemEntity = itemBuffer[0].ItemEntity;
            Assert.AreEqual("item", EntityManager.GetComponentData<Name>(newItemEntity).Value);
            Assert.AreEqual(1, EntityManager.GetComponentData<Count>(newItemEntity).Value);
        }
    }
}