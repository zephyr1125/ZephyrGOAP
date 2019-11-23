using DOTS.Component;
using DOTS.Struct;
using DOTS.System;
using NUnit.Framework;
using Unity.Collections;

namespace DOTS.Test
{
    public class TestCurrentStatesHelper : TestBase
    {
        private CurrentStatesHelper _system;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.GetOrCreateSystem<CurrentStatesHelper>();
        }

        //创建entity
        [Test]
        public void CreateEntity()
        {
            _system.Update();

            var query = EntityManager.CreateEntityQuery(typeof(CurrentStates));
            Assert.AreEqual(1, query.CalculateEntityCount());
        }

        //在一帧结束Simulation时移除entity
        [Test]
        public void RemoveEntityAtSimulationEnd()
        {
            _system.Update();
            _system._removeECBufferSystem.Update();
            EntityManager.CompleteAllJobs();

            var query = EntityManager.CreateEntityQuery(typeof(CurrentStates));
            Assert.AreEqual(0, query.CalculateEntityCount());
        }

        //提供引用
        [Test]
        public void OffersStaticReferenceToEntity()
        {
            _system.Update();

            var query = EntityManager.CreateEntityQuery(typeof(CurrentStates));

            Assert.AreEqual(1, query.CalculateEntityCount());
            var entities = query.ToEntityArray(Allocator.TempJob);

            Assert.AreEqual(entities[0], CurrentStatesHelper.CurrentStatesEntity);

            entities.Dispose();
        }

        //创建StateBuffer
        [Test]
        public void CreateBuffer()
        {
            _system.Update();

            Assert.IsTrue(EntityManager.HasComponent<State>(
                CurrentStatesHelper.CurrentStatesEntity));
        }
    }
}