using DOTS.Component.Trait;
using DOTS.Struct;
using DOTS.System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Test
{
    public class TestGoalPlanningSystemCheckNodes : TestBase
    {
        //CurrentStates直接足以满足goal时，直接完成此goal

        private GoalPlanningSystem _system;

        private Entity _agentEntity, _targetEntity;

        private NativeList<Node> _uncheckedNodes;
        private NativeList<Node> _unexpandedNodes;
        private NodeGraph _nodeGraph;
        private StateGroup _currentStates;
        private Node _goalNode;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _agentEntity = EntityManager.CreateEntity();
            _targetEntity = EntityManager.CreateEntity();
            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            
            _uncheckedNodes = new NativeList<Node>(Allocator.Persistent);
            _unexpandedNodes = new NativeList<Node>(Allocator.Persistent);
            _nodeGraph = new NodeGraph(1, Allocator.Persistent);
            _currentStates = new StateGroup(1, Allocator.Persistent);
            
            var goalStates = new StateGroup(1, Allocator.Temp){new State
            {
                Target = _targetEntity,
                Trait = typeof(Inventory),
                Value = new NativeString64("test"),
            }};
            _goalNode = new Node(ref goalStates);
            
            _nodeGraph.SetGoalNode(_goalNode, ref goalStates);
            
            _uncheckedNodes.Add(_goalNode);
            
            _currentStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(Inventory),
                Value = new NativeString64("test"),
            });
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _uncheckedNodes.Dispose();
            _unexpandedNodes.Dispose();
            _nodeGraph.Dispose();
            _currentStates.Dispose();
        }

        [Test]
        public void CheckNodes_FoundPlan_LinkToStartNode()
        {
            var state = new State
            {
                Target = _agentEntity,
                Trait = typeof(Inventory),
                Value = new NativeString64("test"),
            };
            var node = new Node(ref state);

            _nodeGraph.AddRouteNode(node, ref state, _goalNode,
                new NativeString64("route"));
            _uncheckedNodes.Add(node);

            _system.CheckNodes(ref _uncheckedNodes, ref _nodeGraph, ref _currentStates,
                ref _unexpandedNodes);
            
            Assert.AreEqual(3, _nodeGraph.Length());
            var startParents = _nodeGraph.GetEdgeToParents(_nodeGraph.GetStartNode());
            startParents.MoveNext();
            Assert.AreEqual(node, startParents.Current.Parent);
        }

        [Test]
        public void OnlyGoalInNodeGraph_GoalIntoUnExpandedNodes()
        {
            _system.CheckNodes(ref _uncheckedNodes, ref _nodeGraph, ref _currentStates,
                ref _unexpandedNodes);
            
            Assert.AreEqual(1, _nodeGraph.Length());
            Assert.AreEqual(1, _unexpandedNodes.Length);
            Assert.AreEqual(_goalNode, _unexpandedNodes[0]);
        }

        [Test]
        public void NotFoundPlan_AddToUnExpandedNodes()
        {
            var state = new State
            {
                Target = _agentEntity,
                Trait = typeof(GatherStation),
                Value = new NativeString64("test"),
            };
            var node = new Node(ref state);

            _nodeGraph.AddRouteNode(node, ref state, _goalNode,
                new NativeString64("route"));
            _uncheckedNodes.Add(node);

            _system.CheckNodes(ref _uncheckedNodes, ref _nodeGraph, ref _currentStates,
                ref _unexpandedNodes);
            
            Assert.AreEqual(2, _nodeGraph.Length());
            Assert.AreEqual(2, _unexpandedNodes.Length);
            Assert.AreEqual(node, _unexpandedNodes[1]);
        }
    }
}