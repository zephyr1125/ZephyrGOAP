using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.SensorSystem;

namespace Zephyr.GOAP.Test.SensorSystem
{
    public class TestCollectorSensorSystem : TestBase
    {
        private CollectorSensorSystem _system;

        private Entity _collectorEntity, _rawSourceEntity;

        private CurrentStatesHelper _currentStatesHelper;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _currentStatesHelper = World.GetOrCreateSystem<CurrentStatesHelper>();
            _currentStatesHelper.Update();

            _system = World.GetOrCreateSystem<CollectorSensorSystem>();
            _collectorEntity = EntityManager.CreateEntity();
            _rawSourceEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_collectorEntity, new CollectorTrait());
            EntityManager.AddComponentData(_collectorEntity, new Translation());
            
            var currentStateBuffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            currentStateBuffer.Add(new State
            {
                Target = _rawSourceEntity,
                Position = new float3(5,0,0),
                Trait = typeof(RawSourceTrait),
                ValueString = Utils.RawPeachName
            });
        }

        //写入collector和潜在物品源
        [Test]
        public void SetCollectorAndItemPotentialSourceState()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            Assert.AreEqual(3, buffer.Length);    //1 raw + 1 collector + 1 item potential source
            Assert.AreEqual(new State
            {
                Target = _collectorEntity,
                Trait = typeof(CollectorTrait),
            }, buffer[1]);
            Assert.AreEqual(new State
            {
                Target = _collectorEntity,
                Trait = typeof(ItemPotentialSourceTrait),
                ValueString = Utils.RawPeachName
            }, buffer[2]);
        }
        
        //太远的原料不计入
        [Test]
        public void RawSourceTooFar_NoState()
        {
            var currentStateBuffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            var rawState = currentStateBuffer[0];
            rawState.Position = new float3(99, 0 ,0);
            currentStateBuffer[0] = rawState;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            Assert.AreEqual(2, buffer.Length);    //1 raw + 1 collector
            Assert.AreEqual(new State
            {
                Target = _collectorEntity,
                Trait = typeof(CollectorTrait),
            }, buffer[1]);
        }
    }
}