using NUnit.Framework;
using Unity.Entities;
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
    /// 目标：获得体力
    /// 预期：规划出Eat-Cook序列
    /// </summary>
    public class TestEatCookSequence : TestActionExpandBase<GoalPlanningSystem>
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
                Trait = TypeManager.GetTypeIndex<StaminaTrait>(),
            });
            
            //给BaseStates写入假环境数据：世界里有餐桌、有原料、配方
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Trait = TypeManager.GetTypeIndex<CookerTrait>(),
            });
            buffer.Add(new State
            {
                Target = _itemSourceEntity,
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 1
            });
            buffer.Add(new State
            {
                Target = _diningTableEntity,
                Trait = TypeManager.GetTypeIndex<DiningTableTrait>(),
            });
            
            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }

        [Test]
        public void PlanEatCook()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(_debugger.IsPlanSuccess());
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