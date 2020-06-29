using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;
using Zephyr.GOAP.Struct;
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
            
            //cooker预存好原料
            var itemBuffer = EntityManager.AddBuffer<ContainedItemRef>(_cookerEntity);
            itemBuffer.Add(new ContainedItemRef
            {
                ItemName = "input0",
                ItemEntity = new Entity {Index = 99, Version = 9}
            });
            itemBuffer.Add(new ContainedItemRef
            {
                ItemName = "input1",
                ItemEntity = new Entity {Index = 98, Version = 9}
            });
            
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
                Trait = typeof(ItemDestinationTrait),
                ValueString = "input0",
            });
            bufferStates.Add(new State
            {
                Target = _cookerEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = "input1",
            });
            bufferStates.Add(new State
            {
                Target = _cookerEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = "output",
            });
        }

        [Test]
        public void CookerRemoveInput()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_cookerEntity);
            var items = itemBuffer.ToNativeArray(Allocator.Temp);
            Assert.IsFalse(items.Any(item => item.ItemName.Equals("input0")));
            Assert.IsFalse(items.Any(item => item.ItemName.Equals("input1")));
            items.Dispose();
        }

        [Test]
        public void CookerGotOutput()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_cookerEntity);
            Assert.AreEqual(1, itemBuffer.Length);
            Assert.IsTrue(itemBuffer[0].ItemName.Equals("output"));
        }
    }
}