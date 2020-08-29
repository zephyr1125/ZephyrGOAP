using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.Tests.Mock;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests
{
    public class TestUtils_ItemModify : TestBase
    {
        private MockItemModifySystem _system;

        private Entity _containerEntity, _itemEntity;

        private FixedString32 _itemName;

        public override void SetUp()
        {
            base.SetUp();

            _itemName = ItemNames.Instance().FeastName;

            _system = World.GetOrCreateSystem<MockItemModifySystem>();

            _containerEntity = EntityManager.CreateEntity();
            _itemEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_containerEntity, new ItemContainer());
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
            buffer.Add(new ContainedItemRef{ItemName = _itemName, ItemEntity = _itemEntity});

            EntityManager.AddComponentData(_itemEntity, new Item());
            EntityManager.AddComponentData(_itemEntity, new Name{Value = _itemName});
            EntityManager.AddComponentData(_itemEntity, new Count{Value = 3});
        }

        [Test]
        public void Add_AlreadyHasSameItem_AddAmount()
        {
            _system.ItemName = _itemName;
            _system.Amount = 5;
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(8, EntityManager.GetComponentData<Count>(_itemEntity).Value);
        }

        [Test]
        public void Add_NoSameItem_AddNewItem()
        {
            var newItemName = ItemNames.Instance().RawAppleName;
            _system.ItemName = newItemName;
            _system.Amount = 5;
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(newItemName, buffer[1].ItemName);
            var rawAppleCount = EntityManager.GetComponentData<Count>(buffer[1].ItemEntity);
            Assert.AreEqual(5, rawAppleCount.Value);
        }

        [Test]
        public void Sub_HasItem_ReduceAmount()
        {
            _system.ItemName = _itemName;
            _system.Amount = -2;
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(1, EntityManager.GetComponentData<Count>(_itemEntity).Value);
        }

        [Test]
        public void Sub_NoSuchItem_Fail()
        {
            _system.ItemName = ItemNames.Instance().RawAppleName;
            _system.Amount = -2;
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(3, EntityManager.GetComponentData<Count>(_itemEntity).Value);
        }
    }
}