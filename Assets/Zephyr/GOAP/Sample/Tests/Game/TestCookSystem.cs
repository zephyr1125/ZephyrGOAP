using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.Game.System;
using Zephyr.GOAP.Sample.Game.System.OrderSystem.OrderExecuteSystem;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.Game
{
    public class TestCookSystem : TestBase
    {
        private CookSystem _system;
        private Entity _orderEntity, _executorEntity, _facilityEntity, _itemEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _system = World.GetOrCreateSystem<CookSystem>();
            
            _orderEntity = EntityManager.CreateEntity();
            _executorEntity = EntityManager.CreateEntity();
            _facilityEntity = EntityManager.CreateEntity();
            _itemEntity = EntityManager.CreateEntity();

            //order上要有Order与CookOrder
            EntityManager.AddComponentData(_orderEntity, new Order
            {
                ExecutorEntity = _executorEntity,
                FacilityEntity = _facilityEntity,
                ItemName = ItemNames.Instance().RoastAppleName,
                Amount = 1
            });
            EntityManager.AddComponentData(_orderEntity, new CookOrder());
            EntityManager.AddComponentData(_orderEntity, new OrderReadyToExecute());
            
            //executor上要有CookAction
            EntityManager.AddComponentData(_executorEntity, new CookAction{Level = 1});
            
            //facility上要有物品
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_facilityEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = _itemEntity,
                ItemName = ItemNames.Instance().RawAppleName
            });
            
            //物品
            EntityManager.AddComponentData(_itemEntity, new Item());
            EntityManager.AddComponentData(_itemEntity, new Count{Value = 1});

            var sensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            sensorSystem.Update();
        }

        [Test]
        public void StateToExecuting_After_First_Update()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<OrderExecuting>(_orderEntity));
        }

        [Test]
        public void Execute_After_Period()
        {
            World.SetTime(new TimeData(0, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            World.SetTime(new TimeData(999, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            //原料移除数量
            Assert.Zero(EntityManager.GetComponentData<Count>(_itemEntity).Value);
            //设施增加产物
            var buffer = EntityManager.GetBuffer<ContainedItemRef>(_facilityEntity);
            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(ItemNames.Instance().RoastAppleName, buffer[1].ItemName);
            var outputItemEntity = buffer[1].ItemEntity;
            Assert.IsTrue(EntityManager.HasComponent<Item>(outputItemEntity));
            Assert.AreEqual(1, EntityManager.GetComponentData<Count>(outputItemEntity).Value);
            //移除StateComponent
            Assert.IsFalse(EntityManager.HasComponent<OrderExecuting>(_orderEntity));
            //Order减少需求数量
            Assert.Zero(EntityManager.GetComponentData<Order>(_orderEntity).Amount);
        }
    }
}