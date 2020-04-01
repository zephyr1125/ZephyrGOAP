using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Test.ActionExpand
{
    public class TestMultiStateGoal : TestActionExpandBase
    {
        private Entity _itemDestinationEntity, _itemSourceAEntity, _itemSourceBEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _itemDestinationEntity = EntityManager.CreateEntity();
            _itemSourceAEntity = EntityManager.CreateEntity();
            _itemSourceBEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            
            var buffer = EntityManager.AddBuffer<State>(_agentEntity);
            buffer.Add(new State
            {
                Target = _itemDestinationEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = "item_a"
            });
            buffer.Add(new State
            {
                Target = _itemDestinationEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = "item_b"
            });

            buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _itemSourceAEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = "item_a"
            });
            buffer.Add(new State
            {
                Target = _itemSourceBEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = "item_b"
            });

        }
        
        //规划出完整运输
        [Test]
        public void PlanCorrect()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var children = _debugger.GetChildren(_debugger.GoalNodeLog);
        }
    }
}