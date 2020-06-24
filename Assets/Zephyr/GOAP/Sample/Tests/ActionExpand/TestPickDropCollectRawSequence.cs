using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    /// <summary>
    /// 目标：Collector提供指定物品
    /// 期望：PickRaw + DropRaw + Collect
    /// </summary>
    public class TestPickDropCollectRawSequence : TestActionExpandBase
    {
        private Entity _collectorEntity, _rawSourceEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _collectorEntity = EntityManager.CreateEntity();
            _rawSourceEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new CollectAction());
            EntityManager.AddComponentData(_agentEntity, new PickRawAction());
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());
            
            SetGoal(new State
            {
                Target = _collectorEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = Sample.Utils.RawPeachName
            });
            
            //给CurrentStates写入假环境数据：世界里有collector和rawSource
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _collectorEntity,
                Trait = typeof(CollectorTrait),
            });
            buffer.Add(new State
            {
                Target = _collectorEntity,
                Trait = typeof(ItemPotentialSourceTrait),
                ValueString = Sample.Utils.RawPeachName
            });
            buffer.Add(new State
            {
                Target = _rawSourceEntity,
                Position = new float3(5,0,0),
                Trait = typeof(RawSourceTrait),
                ValueString = Sample.Utils.RawPeachName
            });
        }

        /// <summary>
        /// Pick - Drop - Collect
        /// </summary>
        [Test]
        public void PlanSequence()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult;
            Assert.AreEqual(nameof(CollectAction), result[0].name);
            Assert.AreEqual(nameof(DropRawAction), result[1].name);
            Assert.AreEqual(nameof(PickRawAction), result[2].name);
        }

        /// <summary>
        /// rawSource不存在则失败
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

        [Ignore("这里最近的概念是要离接收点最近而非离agent最近，这就需要知道接收点信息，也就是action串前方的信息，现在无法实现")]
        [Test]
        public void MultiRaw_ChooseNearest()
        {
            var newRawEntity = new Entity {Index = 99, Version = 99};
            //增加一个较近的raw，planner应该选择这个
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = newRawEntity,
                Position = new float3(2,0,0),
                Trait = typeof(RawSourceTrait),
                ValueString = Sample.Utils.RawPeachName
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var result = _debugger.PathResult;
            Assert.AreEqual(nameof(CollectAction), result[0].name);
            Assert.AreEqual(nameof(DropRawAction), result[1].name);
            Assert.IsTrue(result[1].preconditions[0].target.Equals(newRawEntity));
        }
    }
}