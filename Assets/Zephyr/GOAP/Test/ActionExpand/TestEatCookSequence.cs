using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.SensorSystem;

namespace Zephyr.GOAP.Test.ActionExpand
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat-Cook序列
    /// </summary>
    public class TestEatCookSequence : TestActionExpandBase
    {

        private Entity _cookerEntity, _diningTableEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _cookerEntity = EntityManager.CreateEntity();
            _diningTableEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new EatAction());
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            });
            
            //给CurrentStates写入假环境数据：世界里有餐桌、cooker有原料、配方
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Trait = typeof(CookerTrait),
            });
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Trait = typeof(ItemDestinationTrait),
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
            Assert.AreEqual(5, pathResult.Length);
            Assert.AreEqual(nameof(EatAction), pathResult[1].name);
            Assert.AreEqual(nameof(DropItemAction), pathResult[2].name);
            Assert.AreEqual(nameof(PickItemAction), pathResult[3].name);
            Assert.AreEqual(nameof(CookAction), pathResult[4].name);
        }
        
        //改变reward设置，规划随之改变
        [Test]
        public void RewardChange_PlanChange()
        {
            var origin = Utils.RoastAppleStamina;
            Utils.RoastAppleStamina = 0.2f;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.GoalNodeLog);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(2, pathResult.Length);
            Assert.AreEqual(nameof(EatAction), pathResult[1].name);

            Utils.RoastAppleStamina = origin;
        }

        /// <summary>
        /// 非明确的precondition需要被明确的所替代
        /// </summary>
        [Test]
        public void ReplaceNonSpecificPreconditions()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var nodes = EntityManager.GetBuffer<Node>(_agentEntity);
            var states = EntityManager.GetBuffer<State>(_agentEntity);

            var eatNodePrefabsMask = nodes[0].PreconditionsBitmask;
            for (var i = 0; i < states.Length; i++)
            {
                if ((eatNodePrefabsMask & ((ulong) 1 << i)) > 0 &&
                    states[i].Trait == typeof(DiningTableTrait))
                {
                    Assert.AreEqual(new Entity{Index = 9, Version = 9}, states[i].Target);
                }
            }
        }
    }
}