using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System.ActionExecuteSystem;

namespace Zephyr.GOAP.Test.Execute
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
                Trait = typeof(RawSourceTrait),
                ValueString = new NativeString64("item"),
            });
            bufferStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(RawTransferTrait),
                ValueString = new NativeString64("item"),
            });
        }

        [Test]
        public void AgentGotItem()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
            Assert.AreEqual(1, itemBuffer.Length);
            Assert.AreEqual(new ContainedItemRef
                {ItemName = new NativeString64("item")}, itemBuffer[0]);
        }
    }
}