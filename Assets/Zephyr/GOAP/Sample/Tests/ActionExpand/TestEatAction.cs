using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat
    /// </summary>
    public class TestEatAction : TestActionExpandBase
    {
        private Entity _diningTableEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _diningTableEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_agentEntity, new EatAction());
            
            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = ComponentType.ReadOnly<StaminaTrait>(),
            });
            
            //给BaseStates写入假环境数据：世界里有餐桌，餐桌上有食物
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _diningTableEntity,
                Trait = ComponentType.ReadOnly<DiningTableTrait>(),
            });
            buffer.Add(new State
            {
                Target = _diningTableEntity,
                Trait = ComponentType.ReadOnly<ItemDestinationTrait>(),
                ValueString = "raw_apple",
                Amount = 1
            });
        }

        [Test]
        public void PlanEat()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[0];
            Assert.AreEqual(nameof(EatAction), result.name);
            Assert.IsTrue(result.preconditions.Any(state => state.target.Equals(_diningTableEntity)));
            Assert.IsTrue(result.preconditions.Any(state=>state.valueString.Equals("raw_apple")));
        }

        [Test]
        public void ChooseBestRewardFood()
        {
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _diningTableEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = "roast_apple",
                Amount = 1
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var preconditions = _debugger.PathResult[0].preconditions;
            Assert.IsTrue(preconditions.Any(state=>state.valueString.Equals("roast_apple")));
        }
    }
}