// using NUnit.Framework;
// using Unity.Entities;
// using Unity.Mathematics;
// using Zephyr.GOAP.Component;
// using Zephyr.GOAP.Component.AgentState;
// using Zephyr.GOAP.System.ActionExecuteManage;
//
// namespace Zephyr.GOAP.Tests.ActionExecuteManage
// {
//     public class TestNavigatingEndSystem : TestBase
//     {
//         private NavigatingEndSystem _system;
//         private Entity _agentEntity, _nodeEntity;
//
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//
//             _system = World.GetOrCreateSystem<NavigatingEndSystem>();
//
//             _agentEntity = EntityManager.CreateEntity();
//             _nodeEntity = EntityManager.CreateEntity();
//             
//             EntityManager.AddComponentData(_agentEntity, new Agent());
//             EntityManager.AddComponentData(_agentEntity, new Navigating{NodeEntity = _nodeEntity});
//         }
//         
//         //改变agent状态
//         [Test]
//         public void NextAgentState()
//         {
//             _system.Update();
//             _system.EcbSystem.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.IsTrue(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
//             Assert.IsFalse(EntityManager.HasComponent<Navigating>(_agentEntity));
//         }
//         
//         //未移动完毕，继续等待
//         [Test]
//         public void WaitForMovingDone()
//         {
//             EntityManager.AddComponentData(_agentEntity, new TargetPosition{Value = new float3(9,0,0)});
//             
//             _system.Update();
//             _system.EcbSystem.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.IsFalse(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
//             Assert.IsTrue(EntityManager.HasComponent<Navigating>(_agentEntity));
//         }
//     }
// }