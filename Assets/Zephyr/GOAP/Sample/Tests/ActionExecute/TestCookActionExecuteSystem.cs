using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExecute
{
    public class TestCookActionExecuteSystem : TestActionExecuteBase
    {
        private CookActionExecuteSystem _system;
        private Entity _cookerEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<CookActionExecuteSystem>();
            
            _cookerEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(CookAction),
                PreconditionsBitmask = 3,   //0,1
                EffectsBitmask = 1 << 2,    //2
            });
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            bufferStates.Add(new State
            {
                Target = _cookerEntity,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString = "input0",
            });
            bufferStates.Add(new State
            {
                Target = _cookerEntity,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString = "input1",
            });
            bufferStates.Add(new State
            {
                Target = _cookerEntity,
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                ValueString = "output",
            });
        }

        [Test]
        public void CreateOrder()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            var orderQuery =
                EntityManager.CreateEntityQuery(typeof(Order), typeof(OrderWatchSystem.OrderWatch));
            Assert.AreEqual(1, orderQuery.CalculateEntityCount());
        }
    }
}