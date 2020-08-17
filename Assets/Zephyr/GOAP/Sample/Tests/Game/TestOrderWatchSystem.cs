using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.Game
{
    public class TestOrderWatchSystem : TestBase
    {
        private OrderWatchSystem _system;
        private Entity _orderEntity, _agentEntity, _nodeEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _system = World.GetOrCreateSystem<OrderWatchSystem>();
            _orderEntity = EntityManager.CreateEntity();
            _agentEntity = EntityManager.CreateEntity();
            _nodeEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_orderEntity, new Order());
            EntityManager.AddComponentData(_orderEntity, new OrderWatchSystem.OrderWatch
            {
                AgentEntity = _agentEntity,
                NodeEntity = _nodeEntity
            });

            EntityManager.AddComponentData(_agentEntity, new ReadyToAct());
            EntityManager.AddComponentData(_nodeEntity, new ActionNodeActing());
            
            //模拟OrderCleanSystem的清理
            EntityManager.DestroyEntity(_orderEntity);
        }

        [Test]
        public void NextStates()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<ActDone>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
            Assert.IsTrue(EntityManager.HasComponent<ActionNodeDone>(_nodeEntity));
            Assert.IsFalse(EntityManager.HasComponent<ActionNodeActing>(_nodeEntity));
        }
        
        [Test]
        public void RemoveOrderWatch()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.Exists(_orderEntity));
        }
    }
}