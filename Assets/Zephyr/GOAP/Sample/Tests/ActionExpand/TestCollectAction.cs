using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{

    /// <summary>
    /// 目标：目标容器有物品
    /// 期望：Collect+Pick+Drop
    /// </summary>
    public class TestCollectAction : TestActionExpandBase
    {
        private Entity _collectorEntity, _itemDestinationEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _collectorEntity = EntityManager.CreateEntity();
            _itemDestinationEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            EntityManager.AddComponentData(_agentEntity, new CollectAction());
            
            SetGoal(new State{
                Target = _itemDestinationEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = Sample.Utils.RawPeachName
            });
            
            //给CurrentStates写入假环境数据：世界里有collector和collector已有原料
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _collectorEntity,
                Position = new float3(5,0,0),
                Trait = typeof(CollectorTrait),
            });
            buffer.Add(new State
            {
                Target = _collectorEntity,
                Trait = typeof(ItemPotentialSourceTrait),
                ValueString = Utils.RawPeachName,
                Amount = 1
            });
            buffer.Add(new State
            {
                Target = _collectorEntity,
                Trait = typeof(RawDestinationTrait),
                ValueString = Utils.RawPeachName,
                Amount = 1
            });
        }

        [Test]
        public void PlanCollect()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult;
            Assert.AreEqual(nameof(DropItemAction), result[0].name);
            Assert.AreEqual(nameof(PickItemAction), result[1].name);
            Assert.AreEqual(nameof(CollectAction), result[2].name);
        }

        /// <summary>
        /// 未指明Target且没有collector则失败
        /// </summary>
        [Test]
        public void NullTargetAndNoCollector_Fail()
        {
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.RemoveAt(0);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<FailedPlanLog>(_goalEntity));
        }

        /// <summary>
        /// collector上没有原料则失败
        /// </summary>
        [Test]
        public void NoRaw_Fail()
        {
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.RemoveAt(2);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<FailedPlanLog>(_goalEntity));
        }

        [Test]
        public void MultiCollector_ChooseNearest()
        {
            var newCollectorEntity = new Entity {Index = 99, Version = 99};
            //增加一个较近的cooker，planner应该选择这个cooker
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = newCollectorEntity,
                Position = new float3(2,0,0),
                Trait = typeof(CollectorTrait),
            });
            buffer.Add(new State
            {
                Target = newCollectorEntity,
                Trait = typeof(ItemPotentialSourceTrait),
                ValueString = Utils.RawPeachName,
            });
            buffer.Add(new State
            {
                Target = newCollectorEntity,
                Trait = typeof(RawDestinationTrait),
                ValueString = Utils.RawPeachName
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult;
            Assert.AreEqual(nameof(DropItemAction), result[0].name);
            Assert.AreEqual(nameof(PickItemAction), result[1].name);
            Assert.AreEqual(nameof(CollectAction), result[2].name);
            Assert.IsTrue(result[2].effects[0].target.Equals(newCollectorEntity));
        }

        //世界里同时有collector和直接物品源时，选择cost最小的方案
        [Test]
        public void CollectorAndItemSource_ChooseBestCost()
        {
            //测试场景：某容器需要roast_apple，另一容器就有提供，agent既可以直接transfer，也可以collect再transfer
            //todo 目前没有把运输距离纳入cost计算，会直接选择transfer，将来需要测试不同距离的情况
            
            var itemSourceEntity = EntityManager.CreateEntity();
            
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = itemSourceEntity,
                Position = new float3(2,0,0),
                Trait = typeof(ItemSourceTrait),
                ValueString = Utils.RawPeachName,
                Amount = 1
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(2, _debugger.PathResult.Length);
            var result = _debugger.PathResult;
            Assert.AreEqual(nameof(DropItemAction), result[0].name);
            Assert.AreEqual(nameof(PickItemAction), result[1].name);
            Assert.IsTrue(result[1].preconditions[0].target.Equals(itemSourceEntity));
        }
    }
}