using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using DOTS.System;
using DOTS.System.SensorSystem;
using NUnit.Framework;
using Unity.Entities;

namespace DOTS.Test.SensorSystem
{
    public class TestRawSourceSensorSystem : TestBase
    {
        private RawSourceSensorSystem _system;

        private Entity _containerEntity;

        private CurrentStatesHelper _currentStatesHelper;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _currentStatesHelper = World.GetOrCreateSystem<CurrentStatesHelper>();
            _currentStatesHelper.Update();

            _system = World.GetOrCreateSystem<RawSourceSensorSystem>();
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

        //写入具有原料的状态
        [Test]
        public void SetRawSourceState()
        {
            _system.Update();
            _system.ECBufferSystem.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new State
            {
                SubjectType = StateSubjectType.Target,
                Target = _containerEntity,
                Trait = typeof(ItemContainerTrait),
                IsPositive = true,
                Value = new NativeString64("test")
            }, buffer[0]);
        }
    }
}