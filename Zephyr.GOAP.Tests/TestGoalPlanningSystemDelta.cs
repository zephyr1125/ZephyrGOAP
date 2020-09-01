using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalState;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests.Mock;

namespace Zephyr.GOAP.Tests
{
    public class TestGoalPlanningSystemDelta : TestActionExpandBase<MockGoalPlanningSystem>
    {
        private EntityQuery _deltaQuery;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _deltaQuery = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DeltaStates>(),
                ComponentType.ReadOnly<State>());

            EntityManager.AddComponentData(_agentEntity, new MockProduceAction());
            
            SetGoal(new State
            {
                Trait = TypeManager.GetTypeIndex<MockTraitA>(),
                Amount = 1
            });

            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Trait = TypeManager.GetTypeIndex<MockTraitB>(),
                Amount = 2
            });
        }

        [Test]
        public void SaveDelta()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(_debugger.IsPlanSuccess());

            var deltaEntities = _deltaQuery.ToEntityArray(Allocator.TempJob);
            var deltas = _deltaQuery.ToComponentDataArray<DeltaStates>(Allocator.TempJob);
            //检查delta
            Assert.AreEqual(1, deltaEntities.Length);
            var delta = deltas[0];
            Assert.IsTrue(EntityManager.HasComponent<Node>(delta.ActionNodeEntity));
            //检查states
            var states = EntityManager.GetBuffer<State>(deltaEntities[0]);
            Assert.AreEqual(1, states.Length);
            var template = new State
            {
                Trait = TypeManager.GetTypeIndex<MockTraitB>(),
                Amount = 2
            };
            Assert.IsTrue(states[0].Equals(template));
            
            deltas.Dispose();
            deltaEntities.Dispose();
        }

        [Test]
        public void BaseState_Minus_Delta()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Utils.NextGoalState<ExecutingGoal, IdleGoal>(_goalEntity, EntityManager, 0);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(_debugger.IsPlanSuccess());
        }
    }
}