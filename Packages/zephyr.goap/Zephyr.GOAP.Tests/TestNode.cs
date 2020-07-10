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
        private StateGroup _states0, _states1;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _states0 = new StateGroup(1, Allocator.Temp){new State
            {
                Target = Entity.Null,
                Trait = typeof(MockTraitA),
                ValueString = "test",
            }};
            _states1 = new StateGroup(1, Allocator.Temp){new State
            {
                Target = Entity.Null,
                Trait = typeof(MockTraitA),
                ValueString = "test",
            }};
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _states0.Dispose();
            _states1.Dispose();
        }

        //state一样的node应当具有一致hashcode并equal
        [Test]
        public void SameStates_NodesAreEqual()
        {
            var nonPrecondition = new StateGroup();
            var nonEffect = new StateGroup();
            var node0 = new Node(ref nonPrecondition, ref nonEffect, ref _states0,
                "node0", 0, 0, 0, Entity.Null);
            var node1 = new Node(ref nonPrecondition, ref nonEffect, ref _states1,
                "node1", 0, 0, 0, Entity.Null);
            
            Assert.IsTrue(node0.Equals(node1));
        }

        [Test]
        public void DifferentState_NodesNotEqual()
        {
            _states1.Add(new State{
                Target = Entity.Null,
                Trait = typeof(MockTraitB),
                ValueString = "test",
            });
            
            var nonPrecondition = new StateGroup();
            var nonEffect = new StateGroup();
            var node0 = new Node(ref nonPrecondition, ref nonEffect, ref _states0,
                "node0", 0, 0, 0, Entity.Null);
            var node1 = new Node(ref nonPrecondition, ref nonEffect, ref _states1,
                "node1", 0, 0, 0, Entity.Null);
            
            Assert.IsFalse(node0.Equals(node1));
        }
    }
}