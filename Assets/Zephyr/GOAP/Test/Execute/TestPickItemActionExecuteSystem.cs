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
//     public class TestPickItemActionExecuteSystem : TestBase
//     {
//         private PickItemActionExecuteSystem _system;
//         private Entity _agentEntity, _containerEntity, _currentStateEntity;
//
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//
//             _system = World.GetOrCreateSystem<PickItemActionExecuteSystem>();
//
//             _agentEntity = EntityManager.CreateEntity();
//             _containerEntity = EntityManager.CreateEntity();
//             _currentStateEntity = EntityManager.CreateEntity();
//             
//             //container预先存好物品
//             var itemBuffer = EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
//             itemBuffer.Add(new ContainedItemRef
//             {
//                 ItemName = new NativeString64("item"),
//                 ItemEntity = new Entity {Index = 99, Version = 9}
//             });
//             
//             EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
//             EntityManager.AddComponentData(_agentEntity, new ReadyToAct());
//             EntityManager.AddComponentData(_agentEntity, new PickItemAction());
//             EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
//             //agent必须带有已经规划好的任务列表
//             var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
//             bufferNodes.Add(new Node
//             {
//                 Name = new NativeString64(nameof(PickItemAction)),
//                 PreconditionsBitmask = 1,
//                 EffectsBitmask = 1 << 1,
//             });
//             var bufferStates = EntityManager.AddBuffer<State>(_agentEntity);
//             bufferStates.Add(new State
//             {
//                 Target = _containerEntity,
//                 Trait = typeof(ItemContainerTrait),
//                 ValueString = new NativeString64("item"),
//             });
//             bufferStates.Add(new State
//             {
//                 Target = _agentEntity,
//                 Trait = typeof(ItemContainerTrait),
//                 ValueString = new NativeString64("item"),
//             });
//             //currentState存好物品状态
//             bufferStates = EntityManager.AddBuffer<State>(_currentStateEntity);
//             bufferStates.Add(new State
//             {
//                 Target = _containerEntity,
//                 Trait = typeof(ItemContainerTrait),
//                 ValueString = new NativeString64("item")
//             });
//             CurrentStatesHelper.CurrentStatesEntity = _currentStateEntity;
//         }
//
//         [Test]
//         public void TargetRemoveItem()
//         {
//             _system.Update();
//             _system.ECBSystem.Update();
//             EntityManager.CompleteAllJobs();
//
//             var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
//             Assert.AreEqual(0, itemBuffer.Length);
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
//             {
//                 ItemName = new NativeString64("item"), ItemEntity = new Entity{Index = 99, Version = 9}
//             }, itemBuffer[0]);
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
//         
//         [Test]
//         public void HasTarget_UseTarget()
//         {
//             var bufferStates = EntityManager.GetBuffer<State>(_agentEntity);
//             bufferStates[0] = new State
//             {
//                 Target = _containerEntity,
//                 Trait = typeof(ItemContainerTrait),
//                 ValueString = new NativeString64("item"),
//             };
//             
//             _system.Update();
//             _system.ECBSystem.Update();
//             EntityManager.CompleteAllJobs();
//             
//             var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
//             Assert.AreEqual(0, itemBuffer.Length);
//             itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
//             Assert.AreEqual(1, itemBuffer.Length);
//             Assert.AreEqual(new ContainedItemRef
//             {
//                 ItemName = new NativeString64("item"), ItemEntity = new Entity{Index = 99, Version = 9}
//             }, itemBuffer[0]);
//         }
//     }
// }