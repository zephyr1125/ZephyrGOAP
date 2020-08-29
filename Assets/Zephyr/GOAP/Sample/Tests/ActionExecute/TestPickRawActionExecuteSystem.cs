using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExecute
{
    public class TestPickRawActionExecuteSystem : TestActionExecuteBase
    {
        private PickRawActionExecuteSystem _system;

        private Entity _rawEntity; 

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<PickRawActionExecuteSystem>();
            _rawEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new PickRawAction());
            EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            EntityManager.AddBuffer<WatchingOrder>(_agentEntity);
            
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(PickRawAction),
                PreconditionsBitmask = 1,
                EffectsBitmask = 1 << 1
            });
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _rawEntity,
                Trait = TypeManager.GetTypeIndex<RawSourceTrait>(),
                ValueString = "item",
                Amount = 1
            });
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<RawTransferTrait>(),
                ValueString = "item",
                Amount = 1
            });
        }

        [Test]
        public void CreateOrder()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            var orderQuery =
                EntityManager.CreateEntityQuery(typeof(Order), typeof(OrderWatchSystem.OrderWatched));
            Assert.AreEqual(1, orderQuery.CalculateEntityCount());
        }

        [Test]
        public void AgentState_To_Acting()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<Acting>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
        }

        /// <summary>
        /// 如果需要2个来源才能满足，要产生2个order
        /// </summary>
        [Test]
        public void Create2Orders_For2RawSources()
        {
            var node = EntityManager.GetComponentData<Node>(_actionNodeEntity);
            node.PreconditionsBitmask += 1 << 2;
            EntityManager.SetComponentData(_actionNodeEntity, node);
            var bufferStates = EntityManager.GetBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _rawEntity,
                Trait = TypeManager.GetTypeIndex<RawSourceTrait>(),
                ValueString = "item",
                Amount = 2
            });
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var orderQuery =
                EntityManager.CreateEntityQuery(typeof(Order), typeof(OrderWatchSystem.OrderWatched));
            var orders = orderQuery.ToComponentDataArray<Order>(Allocator.Temp);
            Assert.AreEqual(2, orders.Length);
            Assert.AreEqual(2, orders[1].Amount);
            orders.Dispose();
        }
    }
}