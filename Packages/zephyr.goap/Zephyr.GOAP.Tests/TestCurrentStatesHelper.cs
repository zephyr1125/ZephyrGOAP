using NUnit.Framework;
using Unity.Collections;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Tests
{
    public class TestBaseStatesHelper : TestBase
    {
        private BaseStatesHelper _system;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.GetOrCreateSystem<BaseStatesHelper>();
        }

        //创建entity
        [Test]
        public void CreateEntity()
        {
            _system.Update();

            var query = EntityManager.CreateEntityQuery(typeof(BaseStates), typeof(State));
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        //在一帧结束Simulation时清空state
        [Test]
        public void ClearStatesAtSimulationEnd()
        {
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State());
            
            _system.Update();
            EntityManager.CompleteAllJobs();

            var states = BaseStatesHelper.GetBaseStates(EntityManager, Allocator.Temp);
            Assert.AreEqual(1, states.Length());
            states.Dispose();
            
            _system._removeECBufferSystem.Update();
            EntityManager.CompleteAllJobs();

            states = BaseStatesHelper.GetBaseStates(EntityManager, Allocator.Temp);
            Assert.AreEqual(0, states.Length());
            states.Dispose();
        }

        //提供引用
        [Test]
        public void OffersStaticReferenceToEntity()
        {
            _system.Update();

            var query = EntityManager.CreateEntityQuery(typeof(BaseStates), typeof(State));

            Assert.AreEqual(1, query.CalculateEntityCount());
            var entities = query.ToEntityArray(Allocator.TempJob);

            Assert.AreEqual(entities[0], BaseStatesHelper.BaseStatesEntity);

            entities.Dispose();
        }

        //创建StateBuffer
        [Test]
        public void CreateBuffer()
        {
            _system.Update();

            Assert.IsTrue(EntityManager.HasComponent<State>(
                BaseStatesHelper.BaseStatesEntity));
        }
    }
}