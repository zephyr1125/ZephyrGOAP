using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Test
{
    public class TestFailedPlanLogCleanSystem : TestBase
    {
        private FailedPlanLogCleanSystem _system;
        private Entity _agentEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<FailedPlanLogCleanSystem>();
            _agentEntity = EntityManager.CreateEntity();

            var buffer = EntityManager.AddBuffer<FailedPlanLog>(_agentEntity);
            buffer.Add(new FailedPlanLog());
        }

        [Test]
        public void TimeUp_Clean()
        {
            World.SetTime(new TimeData(_system.CoolDownTime, 0));
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.Zero(EntityManager.GetBuffer<FailedPlanLog>(_agentEntity).Length);
        }

        [Test]
        public void TimeNotYet_NotClean()
        {
            World.SetTime(new TimeData(0, 0));
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.NotZero(EntityManager.GetBuffer<FailedPlanLog>(_agentEntity).Length);
        }

        [Test]
        public void Remove2Of3Logs_Correct()
        {
            var buffer = EntityManager.GetBuffer<FailedPlanLog>(_agentEntity);
            buffer.Add(new FailedPlanLog{Time = 4});
            buffer.Add(new FailedPlanLog{Time = 1});
            
            World.SetTime(new TimeData(_system.CoolDownTime+1, 0));
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(1, EntityManager.GetBuffer<FailedPlanLog>(_agentEntity).Length);
            Assert.AreEqual(4, EntityManager.GetBuffer<FailedPlanLog>(_agentEntity)[0].Time);
        }
    }
}