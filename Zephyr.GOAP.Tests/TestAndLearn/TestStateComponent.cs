using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Zephyr.GOAP.Tests.Mock;

namespace Zephyr.GOAP.Tests.TestAndLearn
{
    /// <summary>
    /// 研究StateComponent的行动流程
    /// </summary>
    public class TestStateComponent : TestBase
    {
        private MockStateComponentSystem _system;
        private Entity _entity;

        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<MockStateComponentSystem>();
            
            World.SetTime(new TimeData(0, 1));
            _entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(_entity, new MockStateComponentSystem.MockComponent{DestroyPeriod = 5, StartTime = 0});
        }

        [Test]
        public void StateComponentExistAfterDestroy()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            //首先增添StateComponent
            Assert.IsTrue(EntityManager.HasComponent<MockStateComponentSystem.MockStateComponent>(_entity));
            
            World.SetTime(new TimeData(6, 1));
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            //尽管调用了destroyEntity，但因为StateComponent还在，就不会真消灭
            //不过其他component就没了
            Assert.IsTrue(EntityManager.Exists(_entity));
            Assert.IsFalse(EntityManager.HasComponent<MockStateComponentSystem.MockComponent>(_entity));
            Assert.IsTrue(EntityManager.HasComponent<MockStateComponentSystem.MockStateComponent>(_entity));
            
            //移除StateComponent之后，不需要再次调用DestroyEntity,自动消灭了
            World.SetTime(new TimeData(11, 1));
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.Exists(_entity));
        }
    }
}