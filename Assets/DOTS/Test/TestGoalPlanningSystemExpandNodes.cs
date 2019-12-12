using DOTS.Action;
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
            
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            
            _uncheckedNodes = new NativeList<Node>(Allocator.Persistent);
            _unexpandedNodes = new NativeList<Node>(Allocator.Persistent);
            _expandedNodes = new NativeList<Node>(Allocator.Persistent);
            
            _nodeGraph = new NodeGraph(1, Allocator.Persistent);
            _currentStates = new StateGroup(1, Allocator.Persistent);
            
            var goalStates = new StateGroup(1, Allocator.Temp){new State
            {
                SubjectType = StateSubjectType.Target,
                Target = _targetEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("test"),
                IsPositive = true
            }};
            _goalNode = new Node(ref goalStates, new NativeString64("goal"), 0);
            
            _nodeGraph.SetGoalNode(_goalNode, ref goalStates);
            
            _unexpandedNodes.Add(_goalNode);
            
            _currentStates.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("test"),
                IsPositive = true
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
                ref _uncheckedNodes, ref _expandedNodes, 1);
            
            Assert.AreEqual(2, _nodeGraph.Length());
            Assert.AreEqual(1, _uncheckedNodes.Length);
            var states = _nodeGraph.GetNodeStates(_uncheckedNodes[0], Allocator.Temp);
            Assert.AreEqual(1, states.Length());
            Assert.AreEqual(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("test"),
                IsPositive = true
            }, states[0]);
            
            states.Dispose();
        }
        
        //被展开的Node进入已展开列表
        [Test]
        public void OldNodeIntoExpandedList()
        {
            _system.ExpandNodes(ref _unexpandedNodes, ref _stackData, ref _nodeGraph,
                ref _uncheckedNodes, ref _expandedNodes, 1);
            
            Assert.AreEqual(2, _nodeGraph.Length());
            Assert.AreEqual(1, _expandedNodes.Length);
            Assert.AreEqual(_goalNode, _expandedNodes[0]);
            var states = _nodeGraph.GetNodeStates(_expandedNodes[0], Allocator.Temp);
            Assert.AreEqual(1, states.Length());
            Assert.AreEqual(new State
            {
                SubjectType = StateSubjectType.Target,
                Target = _targetEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("test"),
                IsPositive = true
            }, states[0]);
            
            states.Dispose();
        }
        
        //未展开列表清空
        [Test]
        public void ClearUnExpandedList()
        {
            _system.ExpandNodes(ref _unexpandedNodes, ref _stackData, ref _nodeGraph,
                ref _uncheckedNodes, ref _expandedNodes, 1);
            
            Assert.AreEqual(0, _unexpandedNodes.Length);
        }
        
        //不具备合适action的agent不进行expand
        [Test]
        public void NoSuitAction_NoExpand()
        {
            EntityManager.RemoveComponent<DropItemAction>(_agentEntity);
            
            _system.ExpandNodes(ref _unexpandedNodes, ref _stackData, ref _nodeGraph,
                ref _uncheckedNodes, ref _expandedNodes, 1);
            
            Assert.AreEqual(1, _nodeGraph.Length());
            Assert.AreEqual(1, _expandedNodes.Length);
            Assert.AreEqual(0, _unexpandedNodes.Length);
            Assert.AreEqual(0, _uncheckedNodes.Length);
            var states = _nodeGraph.GetNodeStates(_expandedNodes[0], Allocator.Temp);
            Assert.AreEqual(1, states.Length());
            Assert.AreEqual(new State
            {
                SubjectType = StateSubjectType.Target,
                Target = _targetEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("test"),
                IsPositive = true
            }, states[0]);
            
            states.Dispose();
        }
    }
}