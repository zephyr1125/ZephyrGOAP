using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.SensorSystem;
using Zephyr.GOAP.Test.Debugger;

namespace Zephyr.GOAP.Test.ActionExpand
{
    /// <summary>
    /// 目标：wander
    /// 预期：规划出wander
    /// </summary>
    public class TestWanderAction : TestBase
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
            EntityManager.SetName(_agentEntity, "test");

            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddComponentData(_agentEntity, new WanderAction());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(WanderTrait),
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
        public void PlanWander()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            Debug.Log(_debugger.GoalNodeView);
            Assert.AreEqual(1, _debugger.GoalNodeView.Children[0].States.Length);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(nameof(WanderAction), _debugger.PathResult[1].Name);
            Debug.Log(pathResult);
        }
    }
}