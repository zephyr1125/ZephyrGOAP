using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Logger;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.SensorSystem;

namespace Zephyr.GOAP.Test.ActionExpand
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出Eat
    /// </summary>
    public class TestCookAction : TestActionExpandBase
    {
        private Entity _cookerEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _cookerEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            
            SetGoal(new State
            {
                Target = _cookerEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = Utils.RoastPeachName
            });
            
            //给CurrentStates写入假环境数据：世界里有cooker和recipe,cooker有原料
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Position = new float3(5,0,0),
                Trait = typeof(CookerTrait),
            });
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = new NativeString64(Utils.RawPeachName),
            });
            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }

        [Test]
        public void PlanCook()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(CookAction), result.name);
            Assert.IsTrue(result.states[0].target.Equals(_cookerEntity));
        }

        //对未指明Target的goal进行规划
        [Test]
        public void PlanCookForNullTarget()
        {
            var goal = GetGoal();
            goal.State.Target = Entity.Null;
            SetGoal(goal.State);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(CookAction), result.name);
            Assert.IsTrue(result.states[0].target.Equals(_cookerEntity));
        }

        /// <summary>
        /// 未指明Target且没有cooker则失败
        /// </summary>
        [Test]
        public void NullTargetAndNoCooker_Fail()
        {
            var goal = GetGoal();
            goal.State.Target = Entity.Null;
            SetGoal(goal.State);
            
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.RemoveAt(1);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<FailedPlanLog>((_goalEntity)));
        }

        //对只指定ValueTrait的goal进行规划
        [Test]
        public void PlanCookForValueTrait()
        {
            var goal = GetGoal();
            goal.State.ValueString = new NativeString64();
            goal.State.ValueTrait = typeof(FoodTrait);
            SetGoal(goal.State);

            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(CookAction), result.name);
            Assert.IsTrue(result.states[0].target.Equals(_cookerEntity));
        }
        
        //只指定ValueTrait的，根据配方产生多个node
        [Test]
        public void MultiSettingToMultiNodes()
        {
            var goal = GetGoal();
            goal.State.ValueString = new NativeString64();
            goal.State.ValueTrait = typeof(FoodTrait);
            SetGoal(goal.State);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            //3种有配方的方案
            var children = _debugger.GetChildren(_debugger.GoalNodeLog);
            Assert.AreEqual(3, children.Length);
            Assert.IsTrue(children.Any(nodeLog => nodeLog.states.Any(
                state => state.valueString.Equals("raw_apple"))));
            Assert.IsTrue(children.Any(nodeLog => nodeLog.states.Any(
                state => state.valueString.Equals(Utils.RawPeachName))));
        }

        [Test]
        public void MultiCooker_ChooseNearest()
        {
            var newCookerEntity = new Entity {Index = 99, Version = 99};
            //增加一个较近的cooker，planner应该选择这个cooker
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = newCookerEntity,
                Position = new float3(2,0,0),
                Trait = typeof(CookerTrait),
            });
            buffer.Add(new State
            {
                Target = newCookerEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = new NativeString64(Utils.RawPeachName),
            });
            
            var goal = GetGoal();
            goal.State.Target = Entity.Null;
            SetGoal(goal.State);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(CookAction), result.name);
            Assert.IsTrue(result.states[0].target.Equals(newCookerEntity));
        }

        //世界里同时有cooker和直接物品源时，选择cost最小的方案
        [Test]
        public void CookerAndItemSource_ChooseBestCost()
        {
            //测试场景：某容器需要roast_apple，另一容器就有提供，agent既可以直接transfer，也可以cook再transfer
            //todo 目前没有把运输距离纳入cost计算，会直接选择transfer，将来需要测试不同距离的情况
            
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());

            var itemDestinationEntity = EntityManager.CreateEntity();
            var itemSourceEntity = EntityManager.CreateEntity();
            
            var goal = GetGoal();
            goal.State = new State
            {
                Target = itemDestinationEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = Utils.RoastPeachName
            };
            SetGoal(goal.State);
            
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = itemSourceEntity,
                Position = new float3(2,0,0),
                Trait = typeof(ItemSourceTrait),
                ValueString = Utils.RoastPeachName
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[2];
            Assert.AreEqual(nameof(PickItemAction), result.name);
            Assert.IsTrue(result.states[0].target.Equals(itemSourceEntity));
        }
    }
}