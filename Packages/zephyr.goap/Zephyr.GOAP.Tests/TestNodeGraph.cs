// using NUnit.Framework;
// using Unity.Collections;
// using Unity.Entities;
// using Zephyr.GOAP.Component.Trait;
// using Zephyr.GOAP.Struct;
// using Assert = UnityEngine.Assertions.Assert;
//
// namespace Zephyr.GOAP.Test
// {
//     public class TestNodeGraph : TestBase
//     {
//         private NodeGraph _nodeGraph;
//         private Node _goalNode;
//         
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//             
//             var goalStates = new StateGroup(1, Allocator.Temp){new State
//             {
//                 Target = Entity.Null,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = new NativeString64("test"),
//             }};
//             var goalPreconditions = new StateGroup();
//             var goalEffects = new StateGroup();
//             _goalNode = new Node(goalPreconditions, goalEffects, goalStates,
//                 new NativeString64("goal"), 0, 0, 0, Entity.Null);
//             
//             _nodeGraph = new NodeGraph(1, Allocator.Persistent);
//             _nodeGraph.SetGoalNode(_goalNode, goalStates);
//             
//             goalStates.Dispose();
//         }
//
//         [TearDown]
//         public override void TearDown()
//         {
//             base.TearDown();
//             _nodeGraph.Dispose();
//         }
//         
//         [Test]
//         public void OnlyGoal_Length1()
//         {
//             Assert.AreEqual(1, _nodeGraph.Length());
//         }
//     }
// }