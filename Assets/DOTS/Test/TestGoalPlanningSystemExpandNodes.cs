using DOTS.Component.Trait;
using DOTS.Struct;
using DOTS.System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Test
{
    public class TestGoalPlanningSystemExpandNodes : TestBase
    {
        private GoalPlanningSystem _system;

        private Entity _agentEntity, _targetEntity;

        private NativeList<Node> _uncheckedNodes;
        private NativeList<Node> _unexpandedNodes;
        private NativeList<Node> _expandedNodes;
        private NodeGraph _nodeGraph;
        private StateGroup _currentStates;
        private Node _goalNode;
        private StackData _stackData;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _agentEntity = EntityManager.CreateEntity();
            _targetEntity = EntityManager.CreateEntity();
            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            
            _uncheckedNodes = new NativeList<Node>(Allocator.Persistent);
            _unexpandedNodes = new NativeList<Node>(Allocator.Persistent);
            _expandedNodes = new NativeList<Node>(Allocator.Persistent);
            
            _nodeGraph = new NodeGraph(1, Allocator.Persistent);
            _currentStates = new StateGroup(1, Allocator.Persistent);
            
            var goalStates = new StateGroup(1, Allocator.Temp){new State
            {
                Target = _targetEntity,
                Trait = typeof(RawTrait),
                Value = new NativeString64("test"),
            }};
            _goalNode = new Node(ref goalStates);
            
            _nodeGraph.SetGoalNode(_goalNode, ref goalStates);
            
            _unexpandedNodes.Add(_goalNode);
            
            _currentStates.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(RawTrait),
                Value = new NativeString64("test"),
            });
            
            _stackData = new StackData{AgentEntity = _agentEntity, CurrentStates = _currentStates};
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _uncheckedNodes.Dispose();
            _unexpandedNodes.Dispose();
            _expandedNodes.Dispose();
            _nodeGraph.Dispose();
            _currentStates.Dispose();
//            _stackData.Dispose();
        }
        
        /// <summary>
        /// 新增Node进入未检查列表
        /// </summary>
        [Test]
        public void NewNodeIntoUnCheckedList()
        {
            _system.ExpandNodes(ref _unexpandedNodes, ref _stackData, ref _nodeGraph,
                ref _uncheckedNodes, ref _expandedNodes);
            
            Assert.AreEqual(2, _nodeGraph.Length());
            Assert.AreEqual(1, _uncheckedNodes.Length);
            var states = _nodeGraph.GetStateGroup(_uncheckedNodes[0], Allocator.Temp);
            Assert.AreEqual(1, states.Length());
            Assert.AreEqual(new State
            {
                Target = _agentEntity,
                Trait = typeof(RawTrait),
                Value = new NativeString64("test")
            }, states[0]);
            
            states.Dispose();
        }
        
        //被展开的Node进入已展开列表
        [Test]
        public void OldNodeIntoExpandedList()
        {
            _system.ExpandNodes(ref _unexpandedNodes, ref _stackData, ref _nodeGraph,
                ref _uncheckedNodes, ref _expandedNodes);
            
            Assert.AreEqual(2, _nodeGraph.Length());
            Assert.AreEqual(1, _expandedNodes.Length);
            Assert.AreEqual(_goalNode, _expandedNodes[0]);
            var states = _nodeGraph.GetStateGroup(_expandedNodes[0], Allocator.Temp);
            Assert.AreEqual(1, states.Length());
            Assert.AreEqual(new State
            {
                Target = _targetEntity,
                Trait = typeof(RawTrait),
                Value = new NativeString64("test"),
            }, states[0]);
            
            states.Dispose();
        }
        
        //未展开列表清空
        
        [Test]
        public void ClearUnExpandedList()
        {
            _system.ExpandNodes(ref _unexpandedNodes, ref _stackData, ref _nodeGraph,
                ref _uncheckedNodes, ref _expandedNodes);
            
            Assert.AreEqual(0, _unexpandedNodes.Length);
        }
    }
}