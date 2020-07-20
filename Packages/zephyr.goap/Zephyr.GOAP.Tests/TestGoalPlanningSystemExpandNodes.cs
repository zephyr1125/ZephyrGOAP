// using NUnit.Framework;
// using Unity.Collections;
// using Unity.Entities;
// using Zephyr.GOAP.Action;
// using Zephyr.GOAP.Component.Trait;
// using Zephyr.GOAP.Struct;
// using Zephyr.GOAP.System;
//
// namespace Zephyr.GOAP.Test
// {
//     public class TestGoalPlanningSystemExpandNodes : TestBase
//     {
//         private GoalPlanningSystem _system;
//
//         private Entity _agentEntity, _targetEntity;
//
//         private NativeHashMap<int, Node> _uncheckedNodes;
//         private NativeHashMap<int, Node>.ParallelWriter _uncheckedNodesWriter;
//         private NativeList<Node> _unexpandedNodes;
//         private NativeList<Node> _expandedNodes;
//         private NodeGraph _nodeGraph;
//         private StateGroup _baseStates;
//         private Node _goalNode;
//         private StackData _stackData;
//
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//             _agentEntity = EntityManager.CreateEntity();
//             _targetEntity = EntityManager.CreateEntity();
//             _system = World.GetOrCreateSystem<GoalPlanningSystem>();
//             
//             EntityManager.AddComponentData(_agentEntity, new DropItemAction());
//             
//             
//             _uncheckedNodes = new NativeHashMap<int, Node>(32, Allocator.Persistent);
//             _uncheckedNodesWriter = _uncheckedNodes.AsParallelWriter();
//             _unexpandedNodes = new NativeList<Node>(Allocator.Persistent);
//             _expandedNodes = new NativeList<Node>(Allocator.Persistent);
//             
//             _nodeGraph = new NodeGraph(256, Allocator.Persistent);
//             _baseStates = new StateGroup(1, Allocator.Persistent);
//             
//             var goalStates = new StateGroup(1, Allocator.Temp){new State
//             {
//                 Target = _targetEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             }};
//             var goalPreconditions = new StateGroup();
//             _goalNode = new Node(goalPreconditions, goalStates, goalStates,
//                 new NativeString64("goal"), 0, 0, 0, Entity.Null);
//             
//             _nodeGraph.SetGoalNode(_goalNode, goalStates);
//             
//             _unexpandedNodes.Add(_goalNode);
//             
//             _baseStates.Add(new State
//             {
//                 Target = _agentEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             });
//             
//             _stackData = new StackData{BaseStates = _baseStates};
//         }
//
//         [TearDown]
//         public override void TearDown()
//         {
//             base.TearDown();
//             _uncheckedNodes.Dispose();
//             _unexpandedNodes.Dispose();
//             _expandedNodes.Dispose();
//             _nodeGraph.Dispose();
//             _baseStates.Dispose();
// //            _stackData.Dispose();
//         }
//         
//         /// <summary>
//         /// 新增Node进入未检查列表
//         /// </summary>
//         [Test]
//         public void NewNodeIntoUnCheckedList()
//         {
//             _system.ExpandNodes(_unexpandedNodes, _stackData, _nodeGraph,
//                 _uncheckedNodesWriter, _expandedNodes, 1);
//             
//             Assert.AreEqual(2, _nodeGraph.Length());
//             Assert.AreEqual(1, _uncheckedNodes.Count());
//             var states = _nodeGraph.GetNodeStates(_uncheckedNodes[0], Allocator.Temp);
//             Assert.AreEqual(1, states.Length());
//             Assert.AreEqual(new State
//             {
//                 Target = _agentEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             }, states[0]);
//             
//             states.Dispose();
//         }
//         
//         //被展开的Node进入已展开列表
//         [Test]
//         public void OldNodeIntoExpandedList()
//         {
//             _system.ExpandNodes(_unexpandedNodes, _stackData, _nodeGraph,
//                 _uncheckedNodesWriter, _expandedNodes, 1);
//             
//             Assert.AreEqual(2, _nodeGraph.Length());
//             Assert.AreEqual(1, _expandedNodes.Length);
//             Assert.AreEqual(_goalNode, _expandedNodes[0]);
//             var states = _nodeGraph.GetNodeStates(_expandedNodes[0], Allocator.Temp);
//             Assert.AreEqual(1, states.Length());
//             Assert.AreEqual(new State
//             {
//                 Target = _targetEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             }, states[0]);
//             
//             states.Dispose();
//         }
//         
//         //未展开列表清空
//         [Test]
//         public void ClearUnExpandedList()
//         {
//             _system.ExpandNodes(_unexpandedNodes, _stackData, _nodeGraph,
//                 _uncheckedNodesWriter, _expandedNodes, 1);
//             
//             Assert.AreEqual(0, _unexpandedNodes.Length);
//         }
//         
//         //不具备合适action的agent不进行expand
//         [Test]
//         public void NoSuitAction_NoExpand()
//         {
//             EntityManager.RemoveComponent<DropItemAction>(_agentEntity);
//             
//             _system.ExpandNodes(_unexpandedNodes, _stackData, _nodeGraph,
//                 _uncheckedNodesWriter, _expandedNodes, 1);
//             
//             Assert.AreEqual(1, _nodeGraph.Length());
//             Assert.AreEqual(1, _expandedNodes.Length);
//             Assert.AreEqual(0, _unexpandedNodes.Length);
//             Assert.AreEqual(0, _uncheckedNodes.Count());
//             var states = _nodeGraph.GetNodeStates(_expandedNodes[0], Allocator.Temp);
//             Assert.AreEqual(1, states.Length());
//             Assert.AreEqual(new State
//             {
//                 Target = _targetEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             }, states[0]);
//             
//             states.Dispose();
//         }
//     }
// }