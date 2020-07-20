// using DOTS.Component.Trait;
// using DOTS.Struct;
// using DOTS.System;
// using NUnit.Framework;
// using Unity.Collections;
// using Unity.Entities;
//
// namespace DOTS.Test
// {
//     public class TestGoalPlanningSystemCheckNodes : TestBase
//     {
//         //BaseStates直接足以满足goal时，直接完成此goal
//
//         private GoalPlanningSystem _system;
//
//         private Entity _agentEntity, _targetEntity;
//
//         private NativeList<Node> _uncheckedNodes;
//         private NativeList<Node> _unexpandedNodes;
//         private NodeGraph _nodeGraph;
//         private StateGroup _baseStates;
//         private Node _goalNode;
//
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//             _agentEntity = EntityManager.CreateEntity();
//             _targetEntity = EntityManager.CreateEntity();
//             _system = World.GetOrCreateSystem<GoalPlanningSystem>();
//             
//             _uncheckedNodes = new NativeList<Node>(Allocator.Persistent);
//             _unexpandedNodes = new NativeList<Node>(Allocator.Persistent);
//             _nodeGraph = new NodeGraph(1, Allocator.Persistent);
//             _baseStates = new StateGroup(1, Allocator.Persistent);
//             
//             var goalStates = new StateGroup(1, Allocator.Temp){new State
//             {
//                 Target = _targetEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             }};
//             _goalNode = new Node(goalStates,new NativeString64("goal"),0 , 0);
//             
//             _nodeGraph.SetGoalNode(_goalNode, goalStates);
//             
//             _uncheckedNodes.Add(_goalNode);
//             
//             _baseStates.Add(new State
//             {
//                 Target = _agentEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             });
//         }
//
//         [TearDown]
//         public override void TearDown()
//         {
//             base.TearDown();
//             _uncheckedNodes.Dispose();
//             _unexpandedNodes.Dispose();
//             _nodeGraph.Dispose();
//             _baseStates.Dispose();
//         }
//
//         [Test]
//         public void CheckNodes_FoundPlan_LinkToStartNode()
//         {
//             var state = new State
//             {
//                 Target = _agentEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             };
//             var node = new Node(state, new NativeString64("route"), 0, 1);
//
//             var preconditions = new StateGroup();
//             var effects = new StateGroup();
//             _nodeGraph.AddRouteNode(node, state,
//                 _nodeGraph.NodeStateWriter,
//                 _nodeGraph.PreconditionWriter, _nodeGraph.EffectWriter,
//                 preconditions, effects, _goalNode,
//                 new NativeString64("route"));
//             _uncheckedNodes.Add(node);
//
//             _system.CheckNodes(_uncheckedNodes, _nodeGraph, _baseStates,
//                 _unexpandedNodes);
//             
//             Assert.AreEqual(3, _nodeGraph.Length());
//             var startParents = _nodeGraph.GetEdgeToParents(_nodeGraph.GetStartNode());
//             startParents.MoveNext();
//             Assert.AreEqual(node, startParents.Current.Parent);
//         }
//
//         [Test]
//         public void OnlyGoalInNodeGraph_GoalIntoUnExpandedNodes()
//         {
//             _system.CheckNodes(_uncheckedNodes, _nodeGraph, _baseStates,
//                 _unexpandedNodes);
//             
//             Assert.AreEqual(1, _nodeGraph.Length());
//             Assert.AreEqual(1, _unexpandedNodes.Length);
//             Assert.AreEqual(_goalNode, _unexpandedNodes[0]);
//         }
//
//         [Test]
//         public void NotFoundPlan_AddToUnExpandedNodes()
//         {
//             var state = new State
//             {
//                 Target = _agentEntity,
//                 Trait = TypeManager.GetTypeIndex<GatherStationTrait>(),
//                 ValueString = new NativeString64("test"),
//             };
//             var node = new Node(state, new NativeString64("route"), 0, 1);
//
//             var preconditions = new StateGroup();
//             var effects = new StateGroup();
//             _nodeGraph.AddRouteNode(node, state,
//                 _nodeGraph.NodeStateWriter,
//                 _nodeGraph.PreconditionWriter, _nodeGraph.EffectWriter,
//                 preconditions, effects, _goalNode,
//                 new NativeString64("route"));
//             _uncheckedNodes.Add(node);
//
//             _system.CheckNodes(_uncheckedNodes, _nodeGraph, _baseStates,
//                 _unexpandedNodes);
//             
//             Assert.AreEqual(2, _nodeGraph.Length());
//             Assert.AreEqual(2, _unexpandedNodes.Length);
//             Assert.AreEqual(node, _unexpandedNodes[1]);
//         }
//         
//         //对于SubjectType为Closest的state，如果能找到适合的state，算作
//     }
// }