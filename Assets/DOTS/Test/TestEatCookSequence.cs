using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Struct;
using DOTS.System;
using DOTS.System.SensorSystem;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Test
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat-Cook序列
    /// </summary>
    public class TestEatCookSequence : TestBase
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
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
                IsPositive = true
            });
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
            //给CurrentStates写入假环境数据：自己有原料、世界里有餐桌、制造、配方
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("raw_peach"),
                IsPositive = true
            });
            buffer.Add(new State
            {
                SubjectType = StateSubjectType.Target,
                Target = new Entity{Index = 9, Version = 9},
                Trait = typeof(DiningTableTrait),
                IsPositive = true
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
        public void PlanEatCook()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.NodeGraph);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(3, pathResult.Length);
            Assert.AreEqual(new NativeString64("EatAction"), pathResult[1].Name);
            Assert.AreEqual(new NativeString64("CookAction"), pathResult[2].Name);
        }
    }
}