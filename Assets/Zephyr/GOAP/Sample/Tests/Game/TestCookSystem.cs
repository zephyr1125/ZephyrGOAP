using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.System;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.Game
{
    public class TestCookSystem : TestBase
    {
        private CookSystem _system;
        private Entity _orderEntity, _executorEntity, _facilityEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _system = World.GetOrCreateSystem<CookSystem>();
            
            _orderEntity = EntityManager.CreateEntity();
            _executorEntity = EntityManager.CreateEntity();
            _facilityEntity = EntityManager.CreateEntity();

            //order上要有Order与CookOrder
            EntityManager.AddComponentData(_orderEntity, new Order
            {
                ExecutorEntity = _executorEntity,
                FacilityEntity = _facilityEntity,
                OutputName = ItemNames.Instance().RoastAppleName,
                Amount = 1
            });
            EntityManager.AddComponentData(_orderEntity, new CookOrder());
            
            //executor上要有CookAction
            EntityManager.AddComponentData(_executorEntity, new CookAction{Level = 1});
        }

        [Test]
        public void StateToInited_After_First_Update()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<CookSystem.OrderInited>(_orderEntity));
        }
    }
}