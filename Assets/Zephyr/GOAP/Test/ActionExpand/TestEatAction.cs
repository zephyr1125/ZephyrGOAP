using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Test.Debugger;

namespace Zephyr.GOAP.Test.ActionExpand
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat
    /// </summary>
    public class TestEatAction : TestGoapBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            EntityManager.AddComponentData(_agentEntity, new EatAction());
            
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            });
            
            //给CurrentStates写入假环境数据：自己有食物、世界里有餐桌
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
            });
            buffer.Add(new State
            {
                Target = new Entity{Index = 9, Version = 9},
                Trait = typeof(DiningTableTrait),
            });
        }

        [Test]
        public void MultiFood()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(4, _debugger.GoalNodeView.Children.Count);
        }

        [Test]
        public void ChooseBestRewardFood()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var preconditions = _debugger.PathResult[1].Preconditions;
            Assert.IsTrue(preconditions.Any(state=>state.ValueString.Equals("roast_apple")));
        }
    }
}