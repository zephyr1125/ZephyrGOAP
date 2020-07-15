using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    public class TestDropActionWithScopeGoal : TestActionExpandBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());

            SetGoal(new State
            {
                Target = new Entity {Index = 9, Version = 9},
                Trait = typeof(ItemDestinationTrait),
                ValueTrait = typeof(FoodTrait)
            });
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
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
                nodeLog => nodeLog.requires[0].valueString.Equals(Utils.RawAppleName.ToString())));
            Assert.IsTrue(children.Any(
                nodeLog => nodeLog.requires[0].valueString.Equals(Utils.RoastAppleName.ToString())));
            Assert.IsTrue(children.Any(
                nodeLog => nodeLog.requires[0].valueString.Equals(Utils.RawPeachName.ToString())));
            Assert.IsTrue(children.Any(
                nodeLog => nodeLog.requires[0].valueString.Equals(Utils.RoastPeachName.ToString())));
        }
    }
}