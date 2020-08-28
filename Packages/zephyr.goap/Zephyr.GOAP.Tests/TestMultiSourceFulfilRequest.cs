using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests.Mock;

namespace Zephyr.GOAP.Tests
{
    /// <summary>
    /// 测试当CurrentState里需要多个state的数量加一起才能满足一个request的情况
    /// </summary>
    public class TestMultiSourceFulfilRequest : TestActionExpandBase<MockGoalPlanningSystem>
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
                Amount = 1
            });
            buffer.Add(new State
            {
                Trait = TypeManager.GetTypeIndex<MockTraitB>(),
                Amount = 1
            });
        }

        [Test]
        public void Plan_Success()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(_debugger.IsPlanSuccess());
        }

        /// <summary>
        /// 为每个参与的CurrentState应有各一个delta
        /// </summary>
        [Test]
        public void OneDelta_For_EachCurrentState()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var deltaEntities = _deltaQuery.ToEntityArray(Allocator.TempJob);
            var deltas = _deltaQuery.ToComponentDataArray<DeltaStates>(Allocator.TempJob);
            //检查delta
            Assert.AreEqual(2, deltaEntities.Length);
            var delta = deltas[0];
            Assert.IsTrue(EntityManager.HasComponent<Node>(delta.ActionNodeEntity));
            //检查states
            var states = EntityManager.GetBuffer<State>(deltaEntities[0]);
            Assert.AreEqual(2, states.Length);
            var template = new State
            {
                Trait = TypeManager.GetTypeIndex<MockTraitB>(),
                Amount = 1
            };
            Assert.IsTrue(states[0].Equals(template));
            Assert.IsTrue(states[1].Equals(template));
            
            deltas.Dispose();
            deltaEntities.Dispose();
        }
    }
}