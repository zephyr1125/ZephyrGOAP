// using NUnit.Framework;
// using Unity.Collections;
// using Unity.Entities;
// using Zephyr.GOAP.Action;
// using Zephyr.GOAP.Component;
// using Zephyr.GOAP.Component.AgentState;
// using Zephyr.GOAP.Component.Trait;
// using Zephyr.GOAP.Game.ComponentData;
// using Zephyr.GOAP.Struct;
// using Zephyr.GOAP.System;
// using Zephyr.GOAP.System.ActionExecuteSystem;
//
// namespace Zephyr.GOAP.Test.Execute
// {
//     public class TestPickRawActionExecuteSystem : TestActionExecuteBase
//     {
//         private PickRawActionExecuteSystem _system;
//
//         private Entity _rawEntity; 
//
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//
//             _system = World.GetOrCreateSystem<PickRawActionExecuteSystem>();
//             _rawEntity = EntityManager.CreateEntity();
//             
//             EntityManager.AddComponentData(_agentEntity, new PickRawAction());
//             EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
//             //agent必须带有已经规划好的任务列表
//             var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
//             bufferNodes.Add(new Node
//             {
//                 Name = new NativeString64(nameof(PickRawAction)), 
//                 PreconditionsBitmask = 1,
//                 EffectsBitmask = 1 << 1,
//             });
//             var bufferStates = EntityManager.AddBuffer<State>(_agentEntity);
//             bufferStates.Add(new State
//             {
//                 Target = _rawEntity,
//                 Trait = typeof(RawSourceTrait),
//                 ValueString = new NativeString64("item"),
//             });
//             bufferStates.Add(new State
//             {
//                 Target = _agentEntity,
//                 Trait = typeof(RawTransferTrait),
//                 ValueString = new NativeString64("item"),
//             });
//         }
//
//         [Test]
//         public void AgentGotItem()
//         {
//             _system.Update();
//             _system.ECBSystem.Update();
//             EntityManager.CompleteAllJobs();
//             
//             var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
//             Assert.AreEqual(1, itemBuffer.Length);
//             Assert.AreEqual(new ContainedItemRef
//                 {ItemName = new NativeString64("item")}, itemBuffer[0]);
//         }
//
//         [Test]
//         public void ProgressGoOn()
//         {
//             _system.Update();
//             _system.ECBSystem.Update();
//             EntityManager.CompleteAllJobs();
//
//             var agent = EntityManager.GetComponentData<Agent>(_agentEntity);
//             Assert.AreEqual(new Agent{ExecutingNodeId = 1},agent);
//             Assert.False(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
//             Assert.True(EntityManager.HasComponent<ReadyToNavigate>(_agentEntity));
//         }
//     }
// }