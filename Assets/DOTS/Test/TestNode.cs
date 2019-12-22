using DOTS.Component.Trait;
using DOTS.Struct;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Test
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
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("test"),
            }};
            _states1 = new StateGroup(1, Allocator.Temp){new State
            {
                Target = Entity.Null,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("test"),
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
            var node0 = new Node(ref _states0, new NativeString64("node0"), 0, 0);
            var node1 = new Node(ref _states1, new NativeString64("node1"), 0, 0);
            
            Assert.AreEqual(node1, node0);
            Assert.IsTrue(node0.Equals(node1));
        }

        [Test]
        public void DifferentState_NodesNotEqual()
        {
            _states1.Add(new State{
                Target = Entity.Null,
                Trait = typeof(GatherStationTrait),
                ValueString = new NativeString64("test"),
            });
            
            var node0 = new Node(ref _states0, new NativeString64("node0"), 0, 0);
            var node1 = new Node(ref _states1, new NativeString64("node1"), 0, 0);
            
            Assert.AreNotEqual(node1, node0);
            Assert.IsFalse(node0.Equals(node1));
        }
    }
}