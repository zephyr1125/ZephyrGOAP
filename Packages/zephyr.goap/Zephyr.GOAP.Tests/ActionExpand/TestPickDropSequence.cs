// using NUnit.Framework;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
// using UnityEngine;
// using Zephyr.GOAP.Action;
// using Zephyr.GOAP.Component;
// using Zephyr.GOAP.Component.AgentState;
// using Zephyr.GOAP.Component.Trait;
// using Zephyr.GOAP.Game.ComponentData;
// using Zephyr.GOAP.Struct;
// using Zephyr.GOAP.System;
// using Zephyr.GOAP.System.SensorSystem;
// using Zephyr.GOAP.Test.Debugger;
//
// namespace Zephyr.GOAP.Test.ActionExpand
// {
//     public class TestPickDropSequence : TestActionExpandBase
//     {
//         //一个原料，一个目标容器，一个agent，
//         //实现 Pick -> Drop 的plan序列
//
//         private Entity _itemSourceEntity, _targetContainerEntity;
//         private ItemSourceSensorSystem _sensor;
//
//         private State _goalState;
//         
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//
//             _itemSourceEntity = EntityManager.CreateEntity();
//             _targetContainerEntity = EntityManager.CreateEntity();
//
//             //GOAP数据
//             EntityManager.AddComponentData(_agentEntity, new PickItemAction());
//             EntityManager.AddComponentData(_agentEntity, new DropItemAction());
//             
//             SetGoal(new State
//             {
//                 Target = _targetContainerEntity,
//                 Trait = typeof(ItemDestinationTrait),
//                 ValueString = "item",
//             });
//             
//             var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
//             buffer.Add(new State
//             {
//                 Target = _itemSourceEntity,
//                 Trait = typeof(ItemSourceTrait),
//                 ValueString = "item"
//             });
//         }
//
//         [Test]
//         public void ExpandCorrectNodeGraph()
//         {
//             _system.Update();
//             EntityManager.CompleteAllJobs();
//             
//             //Drop接Goal
//             var dropNodeLog = _debugger.GetChildren(_debugger.GoalNodeLog)[0];
//             Assert.AreEqual(nameof(DropItemAction), dropNodeLog.name);
//             Assert.AreEqual(1, dropNodeLog.states.Length);
//             Assert.IsTrue(dropNodeLog.states[0].Equals(new State
//             {
//                 Target = _agentEntity,
//                 Trait = typeof(ItemTransferTrait),
//                 ValueString = "item",
//             }));
//             
//             //Pick接Drop
//             var pickNodeView = _debugger.GetChildren(dropNodeLog)[0];
//             Assert.AreEqual(nameof(PickItemAction), pickNodeView.name);
//             Assert.AreEqual(1, pickNodeView.states.Length);
//             Assert.IsTrue(pickNodeView.states[0].Equals(new State
//             {
//                 Target = _itemSourceEntity,
//                 Trait = typeof(ItemSourceTrait),
//                 ValueString = "item",
//             }));
//             
//             //start接pick
//             var startNodeView = _debugger.GetChildren(pickNodeView)[0];
//             Assert.AreEqual("start", startNodeView.name);
//             Assert.Zero(startNodeView.states.Length);
//         }
//
//         [Test]
//         public void GetPath()
//         {
//             _system.Update();
//             EntityManager.CompleteAllJobs();
//
//             var pathResult = _debugger.PathResult;
//             Debug.Log(pathResult);
//         }
//         
//         [Test]
//         public void SavePath()
//         {
//             _system.Update();
//             EntityManager.CompleteAllJobs();
//
//             var buffer = EntityManager.GetBuffer<Node>(_agentEntity);
//             Assert.AreEqual(2, buffer.Length);
//         }
//
//         [Test]
//         public void SavePathStates()
//         {
//             _system.Update();
//             EntityManager.CompleteAllJobs();
//
//             var bufferNodes = EntityManager.GetBuffer<Node>(_agentEntity);
//             var bufferStates = EntityManager.GetBuffer<State>(_agentEntity);
//             
//             //1 goal state + 2 precondition + 2 effect
//             Assert.AreEqual(5, bufferStates.Length);
//
//             //0 is pick, 1 is drop
//             var nodePick = bufferNodes[0];
//             var nodeDrop = bufferNodes[1];
//             for (var i = 0; i < bufferStates.Length; i++)
//             {
//                 //nodePick应该只有1个precondition
//                 if ((nodePick.PreconditionsBitmask & (ulong)1 << i) > 0)
//                 {
//                     Assert.AreEqual((ulong)1 << i, nodePick.PreconditionsBitmask);
//                     Assert.IsTrue(bufferStates[i].Equals(new State
//                     {
//                         Target = _itemSourceEntity,
//                         Trait = typeof(ItemSourceTrait),
//                         ValueString = "item",
//                     }));
//                 }
//                 //和一个effect
//                 if ((nodePick.EffectsBitmask & (ulong)1 << i) > 0)
//                 {
//                     Assert.AreEqual((ulong)1 << i, nodePick.EffectsBitmask);
//                     Assert.IsTrue(bufferStates[i].Equals(new State
//                     {
//                         Target = _agentEntity,
//                         Trait = typeof(ItemTransferTrait),
//                         ValueString = "item",
//                     }));
//                 }
//                 //nodeDrop应该只有1个precondition
//                 if ((nodeDrop.PreconditionsBitmask & (ulong)1 << i) > 0)
//                 {
//                     Assert.AreEqual((ulong)1 << i, nodeDrop.PreconditionsBitmask);
//                     Assert.AreEqual(new State
//                     {
//                         Target = _agentEntity,
//                         Position = new float3(),
//                         Trait = typeof(ItemTransferTrait),
//                         ValueString = "item",
//                     }, bufferStates[i]);
//                 }
//                 //和一个effect
//                 if ((nodeDrop.EffectsBitmask & (ulong)1 << i) > 0)
//                 {
//                     Assert.AreEqual((ulong)1 << i, nodeDrop.EffectsBitmask);
//                     Assert.AreEqual(new State
//                     {
//                         Target = _targetContainerEntity,
//                         Trait = typeof(ItemDestinationTrait),
//                         ValueString = "item",
//                     }, bufferStates[i]);
//                 }
//             }
//             
//         }
//
//         [Test]
//         public void AlreadyHasResult_NotRun()
//         {
//             var buffer = EntityManager.AddBuffer<Node>(_agentEntity);
//             buffer.Add(new Node {Name = new NativeString64("exist")});
//             
//             _system.Update();
//             EntityManager.CompleteAllJobs();
//             
//             buffer = EntityManager.GetBuffer<Node>(_agentEntity);
//             Assert.AreEqual(1, buffer.Length);
//         }
//     }
// }