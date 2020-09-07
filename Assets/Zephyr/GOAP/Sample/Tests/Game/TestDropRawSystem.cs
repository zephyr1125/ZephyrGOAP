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
    public class TestDropRawSystem : TestBase
    {
        private DropRawSystem _system;

        private readonly FixedString32 _rawName = ItemNames.Instance().RawAppleName;
        
        private Entity _orderEntity, _executorEntity, _containerEntity, _rawEntity; 

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<DropRawSystem>();
            
            _orderEntity = EntityManager.CreateEntity();
            _executorEntity = EntityManager.CreateEntity();
            _containerEntity = EntityManager.CreateEntity();
            _rawEntity = EntityManager.CreateEntity();

            //order
            EntityManager.AddComponentData(_orderEntity, new Order
            {
                ExecutorEntity = _executorEntity,
                FacilityEntity = _containerEntity,
                ItemName = _rawName,
                Amount = 1
            });
            EntityManager.AddComponentData(_orderEntity, new DropRawOrder());
            EntityManager.AddComponentData(_orderEntity, new OrderReadyToExecute());
            
            EntityManager.AddComponentData(_executorEntity, new DropRawAction());
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_executorEntity);
            buffer.Add(new ContainedItemRef {ItemEntity = _rawEntity, ItemName = _rawName});
            
            EntityManager.AddComponentData(_rawEntity, new Item());
            EntityManager.AddComponentData(_rawEntity, new Name{Value = _rawName});
            EntityManager.AddComponentData(_rawEntity, new Count{Value = 1});

            EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
        }

        [Test]
        public void TargetGotRaw()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var containerBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(1, containerBuffer.Length);
            var rawEntity = containerBuffer[0].ItemEntity;
            Assert.AreEqual(_rawName, EntityManager.GetComponentData<Name>(rawEntity).Value);    
            Assert.AreEqual(1, EntityManager.GetComponentData<Count>(rawEntity).Value);

            Assert.AreEqual(0, EntityManager.GetComponentData<Count>(_rawEntity).Value);
        }
    }
}