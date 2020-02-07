using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.SensorSystem;

namespace Zephyr.GOAP.Test.SensorSystem
{
    public class TestItemSourceSensorSystem : TestBase
    {
        private ItemSourceSensorSystem _system;

        private Entity _containerEntity;

        private CurrentStatesHelper _currentStatesHelper;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _currentStatesHelper = World.GetOrCreateSystem<CurrentStatesHelper>();
            _currentStatesHelper.Update();

            _system = World.GetOrCreateSystem<ItemSourceSensorSystem>();
            _containerEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_containerEntity, new ItemContainerTrait());
            EntityManager.AddComponentData(_containerEntity, new ItemContainer
            {
                Capacity = 1, IsTransferSource = true
            });
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = Entity.Null, ItemName = new NativeString64("test")
            });
        }

        //写入具有物品源的状态
        [Test]
        public void SetItemSourceState()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new State
            {
                Target = _containerEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("test")
            }, buffer[0]);
        }
    }
}