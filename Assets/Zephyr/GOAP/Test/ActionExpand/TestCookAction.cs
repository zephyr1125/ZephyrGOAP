using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.AgentState;
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
    public class TestCookAction : TestGoapBase
    {
        private Entity _cookerEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _cookerEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new CookAction());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _cookerEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = "roast_peach"
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
                ValueString = new NativeString64("raw_peach"),
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
            Assert.AreEqual(nameof(CookAction), result.Name);
            Assert.IsTrue(result.States[0].Target.Equals(_cookerEntity));
        }

        //对未指明Target的goal进行规划
        [Test]
        public void PlanCookForNullTarget()
        {
            var buffer = EntityManager.GetBuffer<State>(_agentEntity);
            var goal = buffer[0];
            goal.Target = Entity.Null;
            buffer[0] = goal;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(CookAction), result.Name);
            Assert.IsTrue(result.States[0].Target.Equals(_cookerEntity));
        }

        /// <summary>
        /// 未指明Target且没有cooker则失败
        /// </summary>
        [Test]
        public void NullTargetAndNoCooker_Fail()
        {
            var buffer = EntityManager.GetBuffer<State>(_agentEntity);
            var goal = buffer[0];
            goal.Target = Entity.Null;
            buffer[0] = goal;
            
            buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.RemoveAt(1);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<NoGoal>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<GoalPlanning>(_agentEntity));
            Assert.Zero(EntityManager.GetBuffer<State>(_agentEntity).Length);
        }

        //对只指定ValueTrait的goal进行规划
        [Test]
        public void PlanCookForValueTrait()
        {
            var buffer = EntityManager.GetBuffer<State>(_agentEntity);
            var goal = buffer[0];
            goal.ValueString = new NativeString64();
            goal.ValueTrait = typeof(FoodTrait);
            buffer[0] = goal;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(CookAction), result.Name);
            Assert.IsTrue(result.States[0].Target.Equals(_cookerEntity));
        }
        
        //只指定ValueTrait的，根据配方产生多个node
        [Test]
        public void MultiSettingToMultiNodes()
        {
            var buffer = EntityManager.GetBuffer<State>(_agentEntity);
            var goal = buffer[0];
            goal.ValueString = new NativeString64();
            goal.ValueTrait = typeof(FoodTrait);
            buffer[0] = goal;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            //2种有配方的方案
            Assert.AreEqual(2, _debugger.GoalNodeView.Children.Count);
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals("raw_apple"))));
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals("raw_peach"))));
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
                ValueString = new NativeString64("raw_peach"),
            });
            
            buffer = EntityManager.GetBuffer<State>(_agentEntity);
            var goal = buffer[0];
            goal.Target = Entity.Null;
            buffer[0] = goal;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(CookAction), result.Name);
            Assert.IsTrue(result.States[0].Target.Equals(newCookerEntity));
        }

        //世界里同时有cooker和直接物品源时，选择cost最小的方案
        [Test]
        public void CookerAndItemSource_ChooseBestCost()
        {
            //测试场景：某容器需要roast_apple，另一容器就有提供，agent既可以直接transfer，也可以cook再transfer
            //todo 目前没有把运输距离纳入cost计算，会直接选择transfer，将来需要测试不同距离的情况
            
            EntityManager.AddComponentData(_agentEntity, new TransferAction());

            var itemDestinationEntity = EntityManager.CreateEntity();
            var itemSourceEntity = EntityManager.CreateEntity();
            
            var buffer = EntityManager.GetBuffer<State>(_agentEntity);
            buffer.Clear();
            buffer.Add(new State
            {
                Target = itemDestinationEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = "roast_peach"
            });
            
            buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = itemSourceEntity,
                Position = new float3(2,0,0),
                Trait = typeof(ItemSourceTrait),
                ValueString = "roast_peach"
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(TransferAction), result.Name);
        }
    }
}