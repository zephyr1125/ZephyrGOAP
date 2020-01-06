using DOTS.Game.ComponentData;
using DOTS.Game.System;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Transforms;

namespace DOTS.Test.Game
{
    public class TestWanderSystem : TestBase
    {
        private WanderSystem _system;
        private Entity _entity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<WanderSystem>();
            _entity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_entity, new Wander {Time = 2});
            EntityManager.AddComponentData(_entity, new Translation());
        }

        [Test]
        public void UpdateOnce_StartWandering()
        {
            World.SetTime(new TimeData(1, 1));
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<Wandering>(_entity));
            Assert.IsTrue(EntityManager.HasComponent<TargetPosition>(_entity));
            
            Assert.AreEqual(1, EntityManager.GetComponentData<Wandering>(_entity).WanderStartTime);
        }

        /// <summary>
        /// 一次移动完毕而时间未到，则创建下一个随机移动
        /// </summary>
        [Test]
        public void MoveDoneAndTimeContinue_NextMove()
        {
            World.SetTime(new TimeData(1, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            EntityManager.RemoveComponent<TargetPosition>(_entity);
            World.SetTime(new TimeData(2, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<Wandering>(_entity));
            Assert.IsTrue(EntityManager.HasComponent<TargetPosition>(_entity));
        }

        [Test]
        public void TimeDone_WanderDone()
        {
            World.SetTime(new TimeData(1, 1));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            EntityManager.RemoveComponent<TargetPosition>(_entity);
            World.SetTime(new TimeData(3, 2));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.HasComponent<Wandering>(_entity));
            Assert.IsFalse(EntityManager.HasComponent<TargetPosition>(_entity));
            Assert.IsFalse(EntityManager.HasComponent<Wander>(_entity));
        }
    }
}