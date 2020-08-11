using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    public class TestDropItemAction : TestActionExpandBase<GoalPlanningSystem>
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());

            SetGoal(new State
            {
                Target = new Entity {Index = 9, Version = 9},
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<FoodTrait>(),
                Amount = 1
            });
            
            World.GetOrCreateSystem<BaseStatesHelper>().Update();
        }
        
        //在使用非特指goal时，要每种物品一个setting
        [Test]
        public void OneSettingPerItemName()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var children = _debugger.GetChildren(_debugger.GoalNodeLog);
            
            Assert.AreEqual(5, children.Length);
            Assert.IsTrue(children.Any(
                nodeLog => nodeLog.requires[0].valueString.Equals(ItemNames.Instance().RawAppleName.ToString())));
            Assert.IsTrue(children.Any(
                nodeLog => nodeLog.requires[0].valueString.Equals(ItemNames.Instance().RoastAppleName.ToString())));
            Assert.IsTrue(children.Any(
                nodeLog => nodeLog.requires[0].valueString.Equals(ItemNames.Instance().RawPeachName.ToString())));
            Assert.IsTrue(children.Any(
                nodeLog => nodeLog.requires[0].valueString.Equals(ItemNames.Instance().RoastPeachName.ToString())));
        }

        //产生同等Amount的precondition
        [Test]
        public void SameAmountOfPrecondition()
        {
            SetGoal(new State
            {
                Target = new Entity {Index = 9, Version = 9},
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<FoodTrait>(),
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