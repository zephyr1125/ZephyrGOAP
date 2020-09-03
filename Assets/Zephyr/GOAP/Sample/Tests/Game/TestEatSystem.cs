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
    public class TestEatSystem : TestBase
    {
        private EatSystem _system;
        private Entity _orderEntity, _executorEntity, _tableEntity, _itemEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _system = World.GetOrCreateSystem<EatSystem>();
            
            _orderEntity = EntityManager.CreateEntity();
            _executorEntity = EntityManager.CreateEntity();
            _tableEntity = EntityManager.CreateEntity();
            _itemEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_executorEntity, new Stamina());
            
            //order上要有Order与EatOrder
            EntityManager.AddComponentData(_orderEntity, new Order
            {
                ExecutorEntity = _executorEntity,
                FacilityEntity = _tableEntity,
                ItemName = ItemNames.Instance().RoastAppleName,
                Amount = 1
            });
            EntityManager.AddComponentData(_orderEntity, new EatOrder());
            EntityManager.AddComponentData(_orderEntity, new OrderReadyToExecute());
            
            //executor上要有EatAction
            EntityManager.AddComponentData(_executorEntity, new EatAction{Level = 1});
            
            //facility上要有物品
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_tableEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = _itemEntity,
                ItemName = ItemNames.Instance().RoastAppleName
            });
            
            //物品
            EntityManager.AddComponentData(_itemEntity, new Item());
            EntityManager.AddComponentData(_itemEntity,
                new Name {Value = ItemNames.Instance().RoastAppleName});
            EntityManager.AddComponentData(_itemEntity, new Count{Value = 1});
        }

        [Test]
        public void TableRemoveFood()
        {
            World.SetTime(new TimeData(0, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            World.SetTime(new TimeData(999, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.Zero(EntityManager.GetComponentData<Count>(_itemEntity).Value);
        }

        [Test]
        public void AgentGotStamina()
        {
            World.SetTime(new TimeData(0, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            World.SetTime(new TimeData(999, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(Utils.GetFoodStamina(ItemNames.Instance().RoastAppleName), 
                EntityManager.GetComponentData<Stamina>(_executorEntity).Value);
        }
    }
}