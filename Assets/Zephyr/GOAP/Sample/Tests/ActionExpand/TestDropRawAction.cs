using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    public class TestDropRawAction : TestActionExpandBase<GoalPlanningSystem>
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());

            SetGoal(new State
            {
                Target = new Entity {Index = 9, Version = 9},
                Trait = TypeManager.GetTypeIndex<RawDestinationTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<FoodTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 1
            });
            
            World.GetOrCreateSystem<BaseStatesHelper>().Update();
        }

        //产生同等Amount的precondition
        [Test]
        public void SameAmountOfPrecondition()
        {
            SetGoal(new State
            {
                Target = new Entity {Index = 9, Version = 9},
                Trait = TypeManager.GetTypeIndex<RawDestinationTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<FoodTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 3
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var children = _debugger.GetChildren(_debugger.GoalNodeLog);
            for (var i = 0; i < children.Length; i++)
            {
                var node = children[i];
                Assert.AreEqual(3, node.requires[0].amount);
            }
        }
    }
}