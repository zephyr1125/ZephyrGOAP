using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.SensorSystem
{
    public class TestCollectorSensorSystem : TestBase
    {
        private CollectorSensorSystem _system;

        private Entity _collectorEntity, _rawSourceEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<CollectorSensorSystem>();
            _collectorEntity = EntityManager.CreateEntity();
            _rawSourceEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_collectorEntity, new CollectorTrait());
            EntityManager.AddComponentData(_collectorEntity, new Translation());

            EntityManager.AddComponentData(_rawSourceEntity,
                new RawSourceTrait {RawName = ItemNames.Instance().RawPeachName});
            EntityManager.AddComponentData(_rawSourceEntity,
                new Translation {Value = new float3(5, 0, 0)});
        }

        //写入collector和潜在物品源
        [Test]
        public void SetCollectorAndItemPotentialSourceState()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            Assert.AreEqual(2, buffer.Length);    //1 collector + 1 item potential source
            Assert.AreEqual(new State
            {
                Target = _collectorEntity,
                Trait = TypeManager.GetTypeIndex<CollectorTrait>(),
            }, buffer[0]);
            Assert.AreEqual(new State
            {
                Target = _collectorEntity,
                Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                ValueString = ItemNames.Instance().RawPeachName
            }, buffer[1]);
        }
        
        //太远的原料不计入
        [Test]
        public void RawSourceTooFar_NoState()
        {
            EntityManager.SetComponentData(_rawSourceEntity,
                new Translation {Value = new float3(CollectorSensorSystem.CollectorRange+1, 0, 0)});
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            Assert.AreEqual(1, buffer.Length);    //1 collector
            Assert.AreEqual(new State
            {
                Target = _collectorEntity,
                Trait = TypeManager.GetTypeIndex<CollectorTrait>(),
            }, buffer[0]);
        }
    }
}