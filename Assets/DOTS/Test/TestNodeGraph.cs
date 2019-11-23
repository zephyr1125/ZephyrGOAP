using DOTS.Component.Trait;
using DOTS.Struct;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Assert = UnityEngine.Assertions.Assert;

namespace DOTS.Test
{
    public class TestNodeGraph : TestBase
    {
        private NodeGraph _nodeGraph;
        private Node _goalNode;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            var goalStates = new StateGroup(1, Allocator.Temp){new State
            {
                Target = Entity.Null,
                Trait = typeof(RawSourceTrait),
                Value = new NativeString64("test"),
            }};
            _goalNode = new Node(ref goalStates);
            
            _nodeGraph = new NodeGraph(1, Allocator.Persistent);
            _nodeGraph.SetGoalNode(_goalNode, ref goalStates);
            
            goalStates.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _nodeGraph.Dispose();
        }
        
        [Test]
        public void OnlyGoal_Length1()
        {
            Assert.AreEqual(1, _nodeGraph.Length());
        }

        [Test]
        public void CorrectLength()
        {
            var state = new State
            {
                Target = Entity.Null,
                Trait = typeof(RawSourceTrait),
                Value = new NativeString64("test"),
            };
            var node = new Node(ref state);
            _nodeGraph.AddRouteNode(node, ref state, _goalNode,
                new NativeString64("next"));
            
            Assert.AreEqual(2, _nodeGraph.Length());
        }
    }
}