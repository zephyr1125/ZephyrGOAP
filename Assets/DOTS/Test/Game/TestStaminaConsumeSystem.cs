using DOTS.Game;
using DOTS.Game.ComponentData;
using DOTS.Game.System;
using NUnit.Framework;
using Unity.Entities;

namespace DOTS.Test.Game
{
    public class TestStaminaConsumeSystem : TestBase
    {
        private StaminaConsumeSystem _system;
        private Entity _entity;

        public override void SetUp()
        {
            base.SetUp();
            _system = World.GetOrCreateSystem<StaminaConsumeSystem>();
            _entity = EntityManager.CreateEntity();

            GameTime.Instance().DeltaSecond = 1;
            
            EntityManager.AddComponentData(_entity, new Stamina{Value = 1, ChangeSpeed = -0.1f});
        }

        [Test]
        public void ChangeOnUpdate()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(0.9f, EntityManager.GetComponentData<Stamina>(_entity).Value);
        }

        [Test]
        public void DontDropBelowZero()
        {
            EntityManager.SetComponentData(_entity, new Stamina{Value = 0.05f, ChangeSpeed = -0.1f});
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(0f, EntityManager.GetComponentData<Stamina>(_entity).Value);
        }
    }
}