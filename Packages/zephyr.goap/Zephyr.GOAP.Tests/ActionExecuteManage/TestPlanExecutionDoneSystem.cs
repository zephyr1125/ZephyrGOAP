using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalState;
using Zephyr.GOAP.System.ActionExecuteManage;

namespace Zephyr.GOAP.Tests.ActionExecuteManage
{
    public class TestPlanExecutionDoneSystem : TestBase
    {
        private PlanExecutionDoneSystem _system;
        private Entity _nodeEntity, _goalEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<PlanExecutionDoneSystem>();
            _nodeEntity = EntityManager.CreateEntity();
            _goalEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_goalEntity, new Goal());
            EntityManager.AddComponentData(_goalEntity, new ExecutingGoal());
            var buffer = EntityManager.AddBuffer<ActionNodeOfGoal>(_goalEntity);
            buffer.Add(new ActionNodeOfGoal{ActionNodeEntity = new Entity{Index = 9, Version = 9}});
        }

        [Test]
        public void RemoveGoal()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.Exists(_goalEntity));
        }

        [Test]
        public void ExistNode_DoNotRemoveGoal()
        {
            var buffer = EntityManager.GetBuffer<ActionNodeOfGoal>(_goalEntity);
            buffer.Add(new ActionNodeOfGoal {ActionNodeEntity = _nodeEntity});
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.Exists(_goalEntity));
        }
    }
}