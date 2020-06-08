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
    public class TestWanderAction : TestActionExpandBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            EntityManager.AddComponentData(_agentEntity, new WanderAction());

            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = typeof(WanderTrait),
            });

            //给CurrentStates写入假环境数据：自己有原料、世界里有cooker和recipe
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = Utils.RawPeachName,
            });
            buffer.Add(new State
            {
                Target = new Entity{Index = 9, Version = 1},
                Trait = typeof(CookerTrait),
            });
            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }

        [Test]
        public void PlanWander()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            Debug.Log(_debugger.GoalNodeLog);
            Assert.AreEqual(1, _debugger.GetChildren(_debugger.GoalNodeLog)[0].states.Length);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(nameof(WanderAction), _debugger.PathResult[1].name);
            Debug.Log(pathResult);
        }
    }
}