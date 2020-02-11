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
    /// 目标：获得体力
    /// 预期：规划出Eat-Cook序列
    /// </summary>
    public class TestEatCookSequence : TestGoapBase
    {

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            EntityManager.AddComponentData(_agentEntity, new EatAction());
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            });
            
            //给CurrentStates写入假环境数据：自己有原料、世界里有餐桌、cooker、配方
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("raw_apple"),
            });
            buffer.Add(new State
            {
                Target = new Entity{Index = 9, Version = 9},
                Trait = typeof(DiningTableTrait),
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
            Utils.RoastPeachStamina = 0.3f;
        }

        [Test]
        public void PlanEatCook()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.GoalNodeView);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(3, pathResult.Length);
            Assert.AreEqual("EatAction", pathResult[1].Name);
            Assert.AreEqual("CookAction", pathResult[2].Name);
        }
        
        //改变reward设置，规划随之改变
        [Test]
        public void RewardChange_PlanChange()
        {
            Utils.RoastAppleStamina = 0.2f;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.GoalNodeView);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(2, pathResult.Length);
            Assert.AreEqual("EatAction", pathResult[1].Name);
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