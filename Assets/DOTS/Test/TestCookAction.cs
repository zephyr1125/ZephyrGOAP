using System.Linq;
using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Struct;
using DOTS.System;
using DOTS.System.SensorSystem;
using DOTS.Test.Debugger;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Test
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat
    /// </summary>
    public class TestCookAction : TestBase
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
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
            });
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
            //给CurrentStates写入假环境数据：自己有原料、世界里有cooker和recipe
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("raw_peach"),
            });
            buffer.Add(new State
            {
                Target = new Entity{Index = 9, Version = 1},
                Trait = typeof(CookerTrait),
            });
            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }
        
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _debugger.Dispose();
        }

        [Test]
        public void PlanCook()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.GoalNodeView);
            Assert.AreEqual(2, _debugger.GoalNodeView.Children[0].States.Length);
            var pathResult = _debugger.PathResult;
            Debug.Log(pathResult);
        }

        [Test]
        public void NoCookerInWorld_PlanFail()
        {
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.RemoveAt(1);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<NoGoal>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<GoalPlanning>(_agentEntity));
            Assert.Zero(EntityManager.GetBuffer<State>(_agentEntity).Length);
        }
        
        //不能被制作的食物要被正确的筛除
        [Test]
        public void RemoveNoCookableFood()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            //物品有4种，只有作为配方产物的2种被保留
            Assert.AreEqual(2, _debugger.GoalNodeView.Children.Count);
        }
        
        //多重setting产生多个node
        [Test]
        public void MultiSettingToMultiNodes()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            //2种有配方的方案
            Assert.AreEqual(2, _debugger.GoalNodeView.Children.Count);
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals(new  NativeString64("raw_apple")))));
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals(new  NativeString64("raw_peach")))));
        }
    }
}