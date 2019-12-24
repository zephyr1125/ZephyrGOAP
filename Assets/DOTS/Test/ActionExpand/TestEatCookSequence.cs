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

namespace DOTS.Test.ActionExpand
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
                Target = _agentEntity,
                Trait = typeof(StaminaTrait),
            });
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
            //给CurrentStates写入假环境数据：自己有原料、世界里有餐桌、cooker、配方
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("raw_peach"),
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
            Utils.RoastPeachReward = 3;
            _debugger.Dispose();
        }

        [Test]
        public void PlanEatCook()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.GoalNodeView);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(3, pathResult.Length);
            Assert.AreEqual(new NativeString64("EatAction"), pathResult[1].Name);
            Assert.AreEqual(new NativeString64("CookAction"), pathResult[2].Name);
        }
        
        //改变reward设置，规划随之改变
        [Test]
        public void RewardChange_PlanChange()
        {
            Utils.RoastPeachReward = 2;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.GoalNodeView);
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(2, pathResult.Length);
            Assert.AreEqual(new NativeString64("EatAction"), pathResult[1].Name);
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

            var eatNodePrefabsMask = nodes[1].PreconditionsBitmask;
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