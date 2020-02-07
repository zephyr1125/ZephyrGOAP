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
    public class TestEatAction : TestBase
    {
        private GoalPlanningSystem _system;
        private Entity _agentEntity;

        private TestGoapDebugger _debugger;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            _debugger = new TestGoapDebugger();
            _system.Debugger = _debugger;
            
            _agentEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddComponentData(_agentEntity, new EatAction());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            });
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
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
        
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _debugger.Dispose();
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