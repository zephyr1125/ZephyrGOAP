using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.SensorSystem;

namespace Zephyr.GOAP.Test.ActionExpand
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat
    /// </summary>
    public class TestCookAction : TestGoapBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
            });
            
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
                state => state.ValueString.Equals("raw_apple"))));
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals("raw_peach"))));
        }
    }
}