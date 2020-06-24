using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat-Cook序列
    /// </summary>
    public class TestEatCookSequence : TestActionExpandBase
    {

        private Entity _cookerEntity, _diningTableEntity, _itemSourceEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _cookerEntity = EntityManager.CreateEntity();
            _diningTableEntity = EntityManager.CreateEntity();
            _itemSourceEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new EatAction());
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            
            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            });
            
            //给CurrentStates写入假环境数据：世界里有餐桌、有原料、配方
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Trait = typeof(CookerTrait),
            });
            buffer.Add(new State
            {
                Target = _itemSourceEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = "raw_apple",
            });
            buffer.Add(new State
            {
                Target = _diningTableEntity,
                Trait = typeof(DiningTableTrait),
            });
            
            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }

        [Test]
        public void PlanEatCook()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.GoalNodeLog);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(6, pathResult.Length);
            Assert.AreEqual(nameof(EatAction), pathResult[0].name);
            Assert.AreEqual(nameof(DropItemAction), pathResult[1].name);
            Assert.AreEqual(nameof(PickItemAction), pathResult[2].name);
            Assert.AreEqual(nameof(CookAction), pathResult[3].name);
            Assert.AreEqual(nameof(DropItemAction), pathResult[4].name);
            Assert.AreEqual(nameof(PickItemAction), pathResult[5].name);
        }
    }
}