using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.Game.System;
using Zephyr.GOAP.Sample.Game.System.OrderSystem;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.Game
{
    public class TestOrderCleanSystem : TestBase
    {
        private OrderCleanSystem _system;
        private Entity _orderEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.GetOrCreateSystem<OrderCleanSystem>();
            _orderEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_orderEntity, new Order{Amount = 0});
            EntityManager.AddComponentData(_orderEntity, new OrderReadyToNavigate());
        }

        [Test]
        public void RemoveZeroOrder()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.Exists(_orderEntity));
        }

        [Test]
        public void DoNotRemoveNonZeroOrder()
        {
            EntityManager.SetComponentData(_orderEntity, new Order{Amount = 1});
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.Exists(_orderEntity));
        }
    }
}