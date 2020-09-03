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
    public class TestPickRawSystem : TestBase
    {
        private PickRawSystem _system;
        private Entity _orderEntity, _executorEntity, _rawContainerEntity, _rawItemEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _system = World.GetOrCreateSystem<PickRawSystem>();
            
            _orderEntity = EntityManager.CreateEntity();
            _executorEntity = EntityManager.CreateEntity();
            _rawContainerEntity = EntityManager.CreateEntity();
            _rawItemEntity = EntityManager.CreateEntity();

            //order上要有Order与PickRawOrder
            EntityManager.AddComponentData(_orderEntity, new Order
            {
                ExecutorEntity = _executorEntity,
                FacilityEntity = _rawContainerEntity,
                ItemName = ItemNames.Instance().RawAppleName,
                Amount = 1
            });
            EntityManager.AddComponentData(_orderEntity, new PickRawOrder());
            EntityManager.AddComponentData(_orderEntity, new OrderReadyToExecute());
            
            //executor上要有PickRawAction
            EntityManager.AddComponentData(_executorEntity, new PickRawAction{Level = 1});
            EntityManager.AddBuffer<ContainedItemRef>(_executorEntity);
            
            //rawContainer上要有物品
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_rawContainerEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = _rawItemEntity,
                ItemName = ItemNames.Instance().RawAppleName
            });
            
            //物品
            EntityManager.AddComponentData(_rawItemEntity, new Item());
            EntityManager.AddComponentData(_rawItemEntity, new Count{Value = 1});
        }

        [Test]
        public void StateToInited_After_First_Update()
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
            Assert.Zero(EntityManager.GetComponentData<Count>(_rawItemEntity).Value);
            //执行者容器增加数量
            var buffer = EntityManager.GetBuffer<ContainedItemRef>(_executorEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(ItemNames.Instance().RawAppleName, buffer[0].ItemName);
            //移除StateComponent
            Assert.IsFalse(EntityManager.HasComponent<OrderExecuting>(_orderEntity));
            //Order减少需求数量
            Assert.Zero(EntityManager.GetComponentData<Order>(_orderEntity).Amount);
        }
    }
}