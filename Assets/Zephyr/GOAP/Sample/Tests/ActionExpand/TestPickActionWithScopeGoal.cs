using System.Linq;
using NUnit.Framework;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    public class TestPickActionWithScopeGoal : TestActionExpandBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            
            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemTransferTrait),
                ValueTrait = typeof(FoodTrait)
            });
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
    }
}