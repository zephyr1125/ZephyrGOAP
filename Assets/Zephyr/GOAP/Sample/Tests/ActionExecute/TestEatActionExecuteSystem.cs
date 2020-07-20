using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExecute
{
    public class TestEatActionExecuteSystem : TestActionExecuteBase
    {
        private EatActionExecuteSystem _system;

        private Entity _diningTableEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<EatActionExecuteSystem>();

            _diningTableEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(_diningTableEntity, new DiningTableTrait());
            EntityManager.AddComponentData(_diningTableEntity, new ItemContainer{IsTransferSource = true});
            var buffer = EntityManager.AddBuffer<ContainedItemRef>(_diningTableEntity);
            //diningTable预存好食物
            buffer.Add(new ContainedItemRef
            {
                ItemName = new NativeString32("roast_apple"),
                ItemEntity = new Entity {Index = 99, Version = 9}
            });
            
            EntityManager.AddComponentData(_agentEntity, new Stamina {Value = 0});
            EntityManager.AddComponentData(_agentEntity, new EatAction());
            
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(EatAction),
                PreconditionsBitmask = (1<<0) + (1<<1),
                EffectsBitmask = 1 << 2,
            });
            var bufferStates = EntityManager.AddBuffer<State>(_actionNodeEntity);
            //preconditions
            bufferStates.Add(new State
            {
                Target = _diningTableEntity,
                Trait = typeof(DiningTableTrait),
            });
            bufferStates.Add(new State
            {
                Target = _diningTableEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = "roast_apple",
            });
            //effect
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            });
        }

        [Test]
        public void TableRemoveFood()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_diningTableEntity);
            Assert.Zero(itemBuffer.Length);
        }

        [Test]
        public void AgentGotStamina()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(Sample.Utils.GetFoodStamina(ItemNames.Instance().RoastAppleName), 
                EntityManager.GetComponentData<Stamina>(_agentEntity).Value);
        }
    }
}