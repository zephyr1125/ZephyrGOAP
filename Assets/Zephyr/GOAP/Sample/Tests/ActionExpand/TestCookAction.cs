using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
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
    /// 目标：获得体力
    /// 预期：规划出Eat
    /// </summary>
    public class TestCookAction : TestActionExpandBase<GoalPlanningSystem>
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
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                ValueString = ItemNames.Instance().RoastPeachName,
                Amount = 1
            });
            
            //给BaseStates写入假环境数据：世界里有cooker和recipe,cooker有原料
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Position = new float3(5,0,0),
                Trait = TypeManager.GetTypeIndex<CookerTrait>(),
            });
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString = ItemNames.Instance().RawPeachName,
                Amount = 99
            });
            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }

        [Test]
        public void PlanCook()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[0];
            Assert.AreEqual(nameof(CookAction), result.name);
            Assert.IsTrue(result.preconditions[0].target.Equals(_cookerEntity));
        }

        //对未指明Target的goal进行规划
        [Test]
        public void PlanCookForNullTarget()
        {
            var goal = GetGoal();
            goal.Require.Target = Entity.Null;
            SetGoal(goal.Require);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[0];
            Assert.AreEqual(nameof(CookAction), result.name);
            Assert.IsTrue(result.preconditions[0].target.Equals(_cookerEntity));
        }

        /// <summary>
        /// 未指明Target且没有cooker则失败
        /// </summary>
        [Test]
        public void NullTargetAndNoCooker_Fail()
        {
            var goal = GetGoal();
            goal.Require.Target = Entity.Null;
            SetGoal(goal.Require);
            
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
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
            goal.Require.ValueString = default;
            goal.Require.ValueTrait = TypeManager.GetTypeIndex<FoodTrait>();
            SetGoal(goal.Require);

            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[0];
            Assert.AreEqual(nameof(CookAction), result.name);
            Assert.IsTrue(result.preconditions[0].target.Equals(_cookerEntity));
        }
        
        //只指定ValueTrait的，根据配方产生多个node
        [Test]
        public void MultiSettingToMultiNodes()
        {
            var goal = GetGoal();
            goal.Require.ValueString = default;
            goal.Require.ValueTrait = TypeManager.GetTypeIndex<FoodTrait>();
            SetGoal(goal.Require);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            //3种有配方的方案
            var children = _debugger.GetChildren(_debugger.GoalNodeLog);
            Assert.AreEqual(3, children.Length);
            Assert.IsTrue(children.Any(nodeLog => nodeLog.preconditions.Any(
                state => state.valueString.Equals(ItemNames.Instance().RawPeachName.ToString()))));
            Assert.IsTrue(children.Any(nodeLog => nodeLog.preconditions.Any(
                state => state.valueString.Equals(ItemNames.Instance().RawAppleName.ToString()))));
        }

        [Test]
        public void MultiCooker_ChooseNearest()
        {
            var newCookerEntity = new Entity {Index = 99, Version = 99};
            //增加一个较近的cooker，planner应该选择这个cooker
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = newCookerEntity,
                Position = new float3(2,0,0),
                Trait = TypeManager.GetTypeIndex<CookerTrait>(),
            });
            buffer.Add(new State
            {
                Target = newCookerEntity,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString = ItemNames.Instance().RawPeachName,
                Amount = 1
            });
            
            var goal = GetGoal();
            goal.Require.Target = Entity.Null;
            SetGoal(goal.Require);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[0];
            Assert.AreEqual(nameof(CookAction), result.name);
            Assert.IsTrue(result.preconditions[0].target.Equals(newCookerEntity));
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
            goal.Require = new State
            {
                Target = itemDestinationEntity,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString = ItemNames.Instance().RoastPeachName,
                Amount = 1
            };
            SetGoal(goal.Require);
            
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = itemSourceEntity,
                Position = new float3(2,0,0),
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                ValueString = ItemNames.Instance().RoastPeachName,
                Amount = 1
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[1];
            Assert.AreEqual(nameof(PickItemAction), result.name);
            Assert.IsTrue(result.preconditions[0].target.Equals(itemSourceEntity));
        }

        //要求生产多份成品会产生对应倍数的原料需求
        [Test]
        public void MultiplyAmountOfRecipe()
        {
            SetGoal(new State
            {
                Target = _cookerEntity,
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                ValueString = ItemNames.Instance().FeastName,
                Amount = 3
            });
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 99
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult[0];
            Assert.AreEqual(2, result.preconditions.Length);
            Assert.AreEqual(6, result.preconditions[0].amount);
            Assert.AreEqual(4, result.preconditions[1].amount);
        }
    }
}