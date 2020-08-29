using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component;
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
            EntityManager.AddComponentData(_orderEntity, new OrderWatchSystem.OrderWatched
            {
                AgentEntity = _agentEntity,
                NodeEntity = _nodeEntity
            });

            var buffer = EntityManager.AddBuffer<WatchingOrder>(_agentEntity);
            buffer.Add(new WatchingOrder{OrderEntity = _orderEntity});
            EntityManager.AddComponentData(_agentEntity, new Acting());
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
            Assert.IsFalse(EntityManager.HasComponent<Acting>(_agentEntity));
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

        /// <summary>
        /// 如果同时监视2个order，则执行完一个之后监视列表减1，而角色不进入下一个state
        /// </summary>
        [Test]
        public void TwoOrdersToWatch_AfterFirst_WatchingReduce_NotNextState()
        {
            var order1Entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(order1Entity, new Order());
            EntityManager.AddComponentData(order1Entity, new OrderWatchSystem.OrderWatched
            {
                AgentEntity = _agentEntity, NodeEntity = _nodeEntity
            });
            var buffer = EntityManager.GetBuffer<WatchingOrder>(_agentEntity);
            buffer.Add(new WatchingOrder{OrderEntity = order1Entity});
            
            Assert.AreEqual(2, buffer.Length);
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            buffer = EntityManager.GetBuffer<WatchingOrder>(_agentEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(order1Entity, buffer[0].OrderEntity);
            
            Assert.IsFalse(EntityManager.HasComponent<ActDone>(_agentEntity));
            Assert.IsTrue(EntityManager.HasComponent<Acting>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ActionNodeDone>(_nodeEntity));
            Assert.IsTrue(EntityManager.HasComponent<ActionNodeActing>(_nodeEntity));
        }
        
        /// <summary>
        /// 如果同时监视2个order，在两个全完成之后，角色进入下一个state，Watching列表为空
        /// </summary>
        [Test]
        public void TwoOrdersToWatch_AfterSecond_WatchingZero_NextState()
        {
            var order1Entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(order1Entity, new Order());
            EntityManager.AddComponentData(order1Entity, new OrderWatchSystem.OrderWatched
            {
                AgentEntity = _agentEntity, NodeEntity = _nodeEntity
            });
            var buffer = EntityManager.GetBuffer<WatchingOrder>(_agentEntity);
            buffer.Add(new WatchingOrder{OrderEntity = order1Entity});
            
            Assert.AreEqual(2, buffer.Length);
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            EntityManager.DestroyEntity(order1Entity);
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            buffer = EntityManager.GetBuffer<WatchingOrder>(_agentEntity);
            Assert.AreEqual(0, buffer.Length);
            
            Assert.IsTrue(EntityManager.HasComponent<ActDone>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<Acting>(_agentEntity));
            Assert.IsTrue(EntityManager.HasComponent<ActionNodeDone>(_nodeEntity));
            Assert.IsFalse(EntityManager.HasComponent<ActionNodeActing>(_nodeEntity));
            
            Assert.IsFalse(EntityManager.Exists(_orderEntity));
        }
    }
}