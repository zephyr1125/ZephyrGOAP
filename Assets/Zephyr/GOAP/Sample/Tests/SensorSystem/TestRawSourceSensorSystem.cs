using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.SensorSystem
{
    public class TestRawSourceSensorSystem : TestBase
    {
        private FixedString32 _rawName;
        
        private RawSourceSensorSystem _system;

        private Entity _rawSourceEntity, _rawItemEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _rawName = ItemNames.Instance().RawPeachName;

            _system = World.GetOrCreateSystem<RawSourceSensorSystem>();
            _rawItemEntity = EntityManager.CreateEntity();
            _rawSourceEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_rawItemEntity, new Item());
            EntityManager.AddComponentData(_rawItemEntity, new Count{Value = 9});

            EntityManager.AddComponentData(_rawSourceEntity,
                new RawSourceTrait {RawName = _rawName});
            EntityManager.AddComponentData(_rawSourceEntity,
                new Translation {Value = new float3(5, 0, 0)});
            EntityManager.AddComponentData(_rawSourceEntity,
                new ItemContainer {IsTransferSource = false});
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_rawSourceEntity);
            buffer.Add(new ContainedItemRef{ItemEntity = _rawItemEntity, ItemName = _rawName});
        }

        //写入rawSource的state
        [Test]
        public void SetCollectorAndItemPotentialSourceState()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(new State
            {
                Target = _rawSourceEntity,
                Position = new float3(5, 0, 0),
                Trait = TypeManager.GetTypeIndex<RawSourceTrait>(),
                ValueString = _rawName,
                Amount = 9
            }, buffer[0]);
        }
    }
}