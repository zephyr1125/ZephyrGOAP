using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat
    /// </summary>
    public class TestEatAction : TestActionExpandBase<GoalPlanningSystem>
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
                Trait = TypeManager.GetTypeIndex<StaminaTrait>(),
            });
            
            //给BaseStates写入假环境数据：世界里有餐桌，餐桌上有食物
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _diningTableEntity,
                Trait = TypeManager.GetTypeIndex<DiningTableTrait>(),
            });
            buffer.Add(new State
            {
                Target = _diningTableEntity,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
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
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString = "roast_apple",
                Amount = 1
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var preconditions = _debugger.PathResult[0].preconditions;
            Assert.IsTrue(preconditions.Any(state=>state.valueString.Equals("roast_apple")));
        }

        /// <summary>
        /// 餐桌食物要移除
        /// </summary>
        [Test]
        public void NoLeftFood()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var deltas = _debugger.PathResult[0].deltas;
            Assert.AreEqual(1, deltas.Length);
            Assert.AreEqual(nameof(ItemDestinationTrait), deltas[0].trait);
            Assert.AreEqual("raw_apple", deltas[0].valueString);
            Assert.AreEqual(1, deltas[0].amount);
        }
    }
}