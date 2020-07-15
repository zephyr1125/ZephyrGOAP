using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.SensorSystem
{
    public class TestCollectorSensorSystem : TestBase
    {
        private CollectorSensorSystem _system;

        private Entity _collectorEntity, _rawSourceEntity;

        private BaseStatesHelper _baseStatesHelper;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _baseStatesHelper = World.GetOrCreateSystem<BaseStatesHelper>();
            _baseStatesHelper.Update();

            _system = World.GetOrCreateSystem<CollectorSensorSystem>();
            _collectorEntity = EntityManager.CreateEntity();
            _rawSourceEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_collectorEntity, new CollectorTrait());
            EntityManager.AddComponentData(_collectorEntity, new Translation());
            
            var baseStateBuffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            baseStateBuffer.Add(new State
            {
                Target = _rawSourceEntity,
                Position = new float3(5,0,0),
                Trait = typeof(RawSourceTrait),
                ValueString = Sample.Utils.RawPeachName
            });
        }

        //写入collector和潜在物品源
        [Test]
        public void SetCollectorAndItemPotentialSourceState()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
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
                ValueString = Sample.Utils.RawPeachName
            }, buffer[2]);
        }
        
        //太远的原料不计入
        [Test]
        public void RawSourceTooFar_NoState()
        {
            var baseStateBuffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            var rawState = baseStateBuffer[0];
            rawState.Position = new float3(CollectorSensorSystem.CollectorRange+1, 0 ,0);
            baseStateBuffer[0] = rawState;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            Assert.AreEqual(2, buffer.Length);    //1 raw + 1 collector
            Assert.AreEqual(new State
            {
                Target = _collectorEntity,
                Trait = typeof(CollectorTrait),
            }, buffer[1]);
        }
    }
}