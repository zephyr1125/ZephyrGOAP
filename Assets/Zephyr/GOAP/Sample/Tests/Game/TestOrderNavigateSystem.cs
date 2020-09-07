using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.Game.System.OrderSystem;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.Game
{
    public class TestOrderNavigateSystem : TestBase
    {
        private OrderNavigateSystem _system;

        private Entity _orderEntity, _facilityEntity, _executorEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<OrderNavigateSystem>();

            _orderEntity = EntityManager.CreateEntity();
            _facilityEntity = EntityManager.CreateEntity();
            _executorEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_facilityEntity,
                new Translation{Value = new float3(5,0,0)});

            EntityManager.AddComponentData(_executorEntity, new Translation());
            
            EntityManager.AddComponentData(_orderEntity, new OrderReadyToNavigate());
            EntityManager.AddComponentData(_orderEntity, new Order
            {
                ExecutorEntity = _executorEntity,
                FacilityEntity = _facilityEntity,
                Amount = 1
            });
        }

        [Test]
        public void FirstUpdate()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<TargetPosition>(_executorEntity));
            Assert.AreEqual(new float3(5,0,0),
                EntityManager.GetComponentData<TargetPosition>(_executorEntity).Value);
            
            Assert.IsFalse(EntityManager.HasComponent<OrderReadyToNavigate>(_orderEntity));
            Assert.IsTrue(EntityManager.HasComponent<OrderNavigating>(_orderEntity));
        }

        [Test]
        public void SecondUpdate()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            EntityManager.RemoveComponent<TargetPosition>(_executorEntity);
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.HasComponent<OrderNavigating>(_orderEntity));
            Assert.IsTrue(EntityManager.HasComponent<OrderReadyToExecute>(_orderEntity));
        }
    }
}