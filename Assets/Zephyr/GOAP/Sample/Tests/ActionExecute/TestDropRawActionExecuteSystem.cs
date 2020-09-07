using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExecute
{
    public class TestDropRawActionExecuteSystem : TestActionExecuteBase
    {
        private DropRawActionExecuteSystem _system;

        private Entity _containerEntity; 

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<DropRawActionExecuteSystem>();
            _containerEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());
            EntityManager.AddBuffer<WatchingOrder>(_agentEntity);
            
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(DropRawAction),
                PreconditionsBitmask = 1,
                EffectsBitmask = 1 << 1
            });
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<RawTransferTrait>(),
                ValueString = "item",
                Amount = 1
            });
            bufferStates.Add(new State
            {
                Target = _containerEntity,
                Trait = TypeManager.GetTypeIndex<RawDestinationTrait>(),
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
    }
}