using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.Tests.Mock;

namespace Zephyr.GOAP.Tests
{
    public class TestNode : TestBase
    {
        private StateGroup _requires0, _requires1;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _requires0 = new StateGroup(1, Allocator.Temp){new State
            {
                Target = Entity.Null,
                Trait = TypeManager.GetTypeIndex<MockTraitA>(),
                ValueString = "test",
            }};
            _requires1 = new StateGroup(1, Allocator.Temp){new State
            {
                Target = Entity.Null,
                Trait = TypeManager.GetTypeIndex<MockTraitA>(),
                ValueString = "test",
            }};
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _requires0.Dispose();
            _requires1.Dispose();
        }

        //state一样的node应当具有一致hashcode并equal
        [Test]
        public void SameStates_NodesAreEqual()
        {
            var nonPrecondition = new StateGroup();
            var nonEffect = new StateGroup();
            var nonDelta = new StateGroup();
            var node0 = new Node(nonPrecondition, nonEffect, _requires0, nonDelta,
                "node0", 0, 0, 0, Entity.Null);
            var node1 = new Node(nonPrecondition, nonEffect, _requires1, nonDelta,
                "node1", 0, 0, 0, Entity.Null);
            
            Assert.IsTrue(node0.Equals(node1));
        }

        [Test]
        public void DifferentState_NodesNotEqual()
        {
            _requires1.Add(new State{
                Target = Entity.Null,
                Trait = TypeManager.GetTypeIndex<MockTraitB>(),
                ValueString = "test",
            });
            
            var nonPrecondition = new StateGroup();
            var nonEffect = new StateGroup();
            var nonDelta = new StateGroup();
            var node0 = new Node(nonPrecondition, nonEffect, _requires0, nonDelta,
                "node0", 0, 0, 0, Entity.Null);
            var node1 = new Node(nonPrecondition, nonEffect, _requires1, nonDelta,
                "node1", 0, 0, 0, Entity.Null);
            
            Assert.IsFalse(node0.Equals(node1));
        }
    }
}