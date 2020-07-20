// using NUnit.Framework;
// using Unity.Core;
// using Unity.Entities;
// using Zephyr.GOAP.Component.GoalManage;
// using Zephyr.GOAP.Component.GoalManage.GoalState;
// using Zephyr.GOAP.Component.Trait;
// using Zephyr.GOAP.Struct;
// using Zephyr.GOAP.System;
// using Zephyr.GOAP.System.SensorSystem;
//
// namespace Zephyr.GOAP.Test
// {
//     /// <summary>
//     /// goal被planning之后要变化状态
//     /// </summary>
//     public class TestGoalPlanningSystemUpdateGoalState : TestActionExpandBase
//     {
//         [SetUp]
//         public override void SetUp()
//         {
//             base.SetUp();
//             
//             var goalState = new State
//             {
//                 Target = _agentEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueTrait = TypeManager.GetTypeIndex<FoodTrait>(),
//             };
//
//             EntityManager.AddComponentData(_goalEntity, new Goal
//             {
//                 GoalEntity = _goalEntity,
//                 State = goalState
//             });
//             EntityManager.AddComponentData(_goalEntity,
//                 new AgentGoal {Agent = _agentEntity});
//             EntityManager.AddComponentData(_goalEntity,
//                 new PlanningGoal());
//             
//             EntityManager.AddComponentData(_agentEntity, new CookAction());
//             var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
//             stateBuffer.Add(goalState);
//             
//             //给BaseStates写入假环境数据：自己有原料、世界里有cooker和recipe
//             var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
//             buffer.Add(new State
//             {
//                 Target = _agentEntity,
//                 Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
//                 ValueString = StringTable.Instance().RawPeachName,
//             });
//             buffer.Add(new State
//             {
//                 Target = new Entity{Index = 9, Version = 1},
//                 Trait = TypeManager.GetTypeIndex<CookerTrait>(),
//             });
//             var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
//             recipeSensorSystem.Update();
//         }
//
//         [Test]
//         public void PlanSuccessful_ChangeGoalState()
//         {
//             _system.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.IsFalse(EntityManager.HasComponent<PlanningGoal>(_goalEntity));
//             Assert.IsTrue(EntityManager.HasComponent<ExecutingGoal>(_goalEntity));
//         }
//
//         [Test]
//         public void PlanFailed_ChangeGoalState()
//         {
//             World.SetTime(new TimeData(9, 0));
//             EntityManager.RemoveComponent<CookAction>(_agentEntity);
//             
//             _system.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.IsFalse(EntityManager.HasComponent<PlanningGoal>(_goalEntity));
//             Assert.IsTrue(EntityManager.HasComponent<PlanFailedGoal>(_goalEntity));
//             
//             var planFailedGoal = EntityManager.GetComponentData<PlanFailedGoal>(_goalEntity);
//             Assert.AreEqual(9, planFailedGoal.Time);
//         }
//
//         [Test]
//         public void PlanFailed_RecordOnAgent()
//         {
//             World.SetTime(new TimeData(9, 0));
//             EntityManager.RemoveComponent<CookAction>(_agentEntity);
//             
//             _system.Update();
//             EntityManager.CompleteAllJobs();
//             
//             Assert.AreEqual(new FailedPlanLog{Time = 9},
//                 EntityManager.GetBuffer<FailedPlanLog>(_agentEntity)[0]);
//         }
//     }
// }