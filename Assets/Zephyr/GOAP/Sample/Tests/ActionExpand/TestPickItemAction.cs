using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    public class TestPickItemAction : TestActionExpandBase<GoalPlanningSystem>
    {
        private Entity _itemEntity;
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _itemEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            
            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<ItemTransferTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 3
            });
            
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _itemEntity,
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 3
            });
        }
        
        //数量一致
        [Test]
        public void SameAmountOfDelta()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(_debugger.IsPlanSuccess());
            
            var children = _debugger.GetChildren(_debugger.GoalNodeLog);
            for (var i = 0; i < children.Length; i++)
            {
                var node = children[i];
                Assert.AreEqual(3, node.deltas[0].amount);
            }
        }
        
        //一个目标源无法满足时要多次Pick(Pick的delta和precondition都为多个)
        [Test]
        public void MultiSourceFulfilRequire()
        {
            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<ItemTransferTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 5
            });
            
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = new Entity{Index = 9, Version = 9},
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 3
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(_debugger.IsPlanSuccess());
            
            Assert.AreEqual(2, _debugger.PathResult[0].deltas.Length);
            Assert.AreEqual(2, _debugger.PathResult[0].preconditions.Length);
        }
    }
}