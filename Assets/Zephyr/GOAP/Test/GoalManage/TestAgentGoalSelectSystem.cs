using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.GoalManage;

namespace Zephyr.GOAP.Test.GoalManage
{
    [TestFixture]
    public class TestAgentGoalSelectSystem : TestBase
    {
        private AgentGoalSelectSystem _system;
        private Entity _agentEntity;

        private Entity _globalGoalItemEntity;

        private State _gloabalItemGoalState;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<AgentGoalSelectSystem>();
            _agentEntity = EntityManager.CreateEntity();
            _globalGoalItemEntity = EntityManager.CreateEntity();

            _gloabalItemGoalState = new State
            {
                Target = new Entity {Index = 9, Version = 9},
                Trait = typeof(ItemContainerTrait),
                ValueString = "item"
            };

            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddComponentData(_agentEntity, new Translation());
            EntityManager.AddBuffer<State>(_agentEntity);
            EntityManager.AddComponentData(_agentEntity, new NoGoal());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());

            EntityManager.AddComponentData(_globalGoalItemEntity, new GlobalGoal());
            EntityManager.AddComponentData(_globalGoalItemEntity,
                new Goal {GoalEntity = _globalGoalItemEntity, State = _gloabalItemGoalState});
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
        }

        [Test]
        public void IncludeGlobalGoals()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.HasComponent<NoGoal>(_agentEntity));
            Assert.IsTrue(EntityManager.HasComponent<GoalPlanning>(_agentEntity));
            var buffer = EntityManager.GetBuffer<State>(_agentEntity);
            Assert.AreEqual(_gloabalItemGoalState, buffer[0]);
            
            Assert.IsTrue(EntityManager.HasComponent<PlanningGoal>(_globalGoalItemEntity));
            Assert.AreEqual(_agentEntity,
                EntityManager.GetComponentData<PlanningGoal>(_globalGoalItemEntity).AgentEntity);
        }
    }
}