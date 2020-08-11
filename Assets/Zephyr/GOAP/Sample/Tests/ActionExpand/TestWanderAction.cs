using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System;
using Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    /// <summary>
    /// 目标：wander
    /// 预期：规划出wander
    /// </summary>
    public class TestWanderAction : TestActionExpandBase<GoalPlanningSystem>
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            EntityManager.AddComponentData(_agentEntity, new WanderAction());

            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<WanderTrait>(),
            });

            //给BaseStates写入假环境数据
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
                ValueString = ItemNames.Instance().RawPeachName,
            });
            buffer.Add(new State
            {
                Target = new Entity{Index = 9, Version = 1},
                Trait = TypeManager.GetTypeIndex<CookerTrait>(),
            });
            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }

        [Test]
        public void PlanWander()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var pathResult = _debugger.PathResult;
            Assert.AreEqual(nameof(WanderAction), _debugger.PathResult[0].name);
            Debug.Log(pathResult);
        }
    }
}