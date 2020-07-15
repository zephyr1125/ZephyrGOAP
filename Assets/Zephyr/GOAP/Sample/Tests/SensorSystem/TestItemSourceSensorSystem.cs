using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.SensorSystem
{
    public class TestItemSourceSensorSystem : TestBase
    {
        private ItemSourceSensorSystem _system;

        private Entity _containerEntity;

        private BaseStatesHelper _baseStatesHelper;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _baseStatesHelper = World.GetOrCreateSystem<BaseStatesHelper>();
            _baseStatesHelper.Update();

            _system = World.GetOrCreateSystem<ItemSourceSensorSystem>();
            _containerEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_containerEntity, new ItemContainerTrait());
            EntityManager.AddComponentData(_containerEntity, new Translation());
            EntityManager.AddComponentData(_containerEntity, new ItemContainer
            {
                Capacity = 1, IsTransferSource = true
            });
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
            buffer.Add(new ContainedItemRef
            {
                ItemEntity = Entity.Null, ItemName = "test"
            });
        }

        //写入具有物品源的状态
        [Test]
        public void SetItemSourceState()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new State
            {
                Target = _containerEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = "test"
            }, buffer[0]);
        }
    }
}