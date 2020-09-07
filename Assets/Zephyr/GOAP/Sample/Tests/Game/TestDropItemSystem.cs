using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.Game.System.OrderSystem.OrderExecuteSystem;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.Game
{
    public class TestDropItemSystem : TestBase
    {
        private DropItemSystem _system;

        private readonly FixedString32 _itemName = ItemNames.Instance().RoastAppleName;
        
        private Entity _orderEntity, _executorEntity, _containerEntity, _itemEntity; 

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<DropItemSystem>();
            
            _orderEntity = EntityManager.CreateEntity();
            _executorEntity = EntityManager.CreateEntity();
            _containerEntity = EntityManager.CreateEntity();
            _itemEntity = EntityManager.CreateEntity();

            //order
            EntityManager.AddComponentData(_orderEntity, new Order
            {
                ExecutorEntity = _executorEntity,
                FacilityEntity = _containerEntity,
                ItemName = _itemName,
                Amount = 1
            });
            EntityManager.AddComponentData(_orderEntity, new DropItemOrder());
            EntityManager.AddComponentData(_orderEntity, new OrderReadyToExecute());
            
            EntityManager.AddComponentData(_executorEntity, new DropItemAction());
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_executorEntity);
            buffer.Add(new ContainedItemRef {ItemEntity = _itemEntity, ItemName = _itemName});
            
            EntityManager.AddComponentData(_itemEntity, new Item());
            EntityManager.AddComponentData(_itemEntity, new Name{Value = _itemName});
            EntityManager.AddComponentData(_itemEntity, new Count{Value = 1});

            EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
        }

        [Test]
        public void TargetGotItem()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var containerBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(1, containerBuffer.Length);
            var itemEntity = containerBuffer[0].ItemEntity;
            Assert.AreEqual(_itemName, EntityManager.GetComponentData<Name>(itemEntity).Value);    
            Assert.AreEqual(1, EntityManager.GetComponentData<Count>(itemEntity).Value);

            Assert.AreEqual(0, EntityManager.GetComponentData<Count>(_itemEntity).Value);
        }
    }
}