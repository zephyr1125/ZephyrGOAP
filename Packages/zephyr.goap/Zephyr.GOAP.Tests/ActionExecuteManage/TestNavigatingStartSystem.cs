// using NUnit.Framework;
// using Unity.Mathematics;
// using Unity.Transforms;
// using Zephyr.GOAP.Component;
// using Zephyr.GOAP.Component.AgentState;
// using Zephyr.GOAP.System.ActionExecuteManage;
// using Entity = Unity.Entities.Entity;
//
// namespace Zephyr.GOAP.Tests.ActionExecuteManage
// {
//     public class TestNavigatingStartSystem : TestBase
//     {
//         private NavigatingStartSystem _system;
//         private Entity _nodeEntity, _agentEntity, _containerEntity;
//
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//
//             _system = World.GetOrCreateSystem<NavigatingStartSystem>();
//
//             _nodeEntity = EntityManager.CreateEntity();
//             _agentEntity = EntityManager.CreateEntity();
//             _containerEntity = EntityManager.CreateEntity();
//             
//             //container要有位置数据
//             EntityManager.AddComponentData(_containerEntity, new Translation{Value = new float3(9,0,0)});
//             
//             EntityManager.AddComponentData(_agentEntity, new Agent());
//             EntityManager.AddComponentData(_agentEntity, new ReadyToNavigate{NodeEntity = _nodeEntity});
//             EntityManager.AddComponentData(_agentEntity, new Translation{Value = float3.zero});
//             
//             //node必须带有已经规划好的任务
//             EntityManager.AddComponentData(_nodeEntity, new Node{NavigatingSubject = _containerEntity});
//         }
//         
//         //为agent赋予移动目标
//         [Test]
//         public void AddTargetPositionToAgent()
//         {
//             _system.Update();
//             _system.EcbSystem.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.AreEqual(new float3(9,0,0),
//                 EntityManager.GetComponentData<TargetPosition>(_agentEntity).Value);
//         }
//         
//         //切换agent状态
//         [Test]
//         public void NextAgentState()
//         {
//             _system.Update();
//             _system.EcbSystem.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.IsTrue(EntityManager.HasComponent<Navigating>(_agentEntity));
//             Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigate>(_agentEntity));
//         }
//         
//         //目标为自身则直接结束
//         [Test]
//         public void TargetIsSelf_ToNextState()
//         {
//             EntityManager.SetComponentData(_nodeEntity,
//                 new Node{NavigatingSubject = _agentEntity});
//             
//             _system.Update();
//             _system.EcbSystem.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.IsTrue(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
//             Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigate>(_agentEntity));
//         }
//         
//         //目标为空则直接结束
//         [Test]
//         public void TargetIsNull_ToNextState()
//         {
//             EntityManager.SetComponentData(_nodeEntity,
//                 new Node{NavigatingSubject = Entity.Null});
//             
//             _system.Update();
//             _system.EcbSystem.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.IsTrue(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
//             Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigate>(_agentEntity));
//         }
//     }
// }