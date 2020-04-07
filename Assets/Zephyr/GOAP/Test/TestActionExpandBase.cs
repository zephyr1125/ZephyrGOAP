using System;
using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Test.Debugger;

namespace Zephyr.GOAP.Test
{
    public class TestActionExpandBase : TestBase
    {
        protected GoalPlanningSystem _system;
        protected Entity _agentEntity, _goalEntity;

        protected TestGoapDebugger _debugger;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            _debugger = new TestGoapDebugger();
            _system.Debugger = _debugger;
            
            _agentEntity = EntityManager.CreateEntity();
            _goalEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddComponentData(_agentEntity, new Translation());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());

            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _debugger.Dispose();
        }

        protected void AddGoal(Entity agentEntity, State goalState, Priority priority, double time)
        {
            var newGoalEntity = EntityManager.CreateEntity();
            if (agentEntity != Entity.Null)
            {
                EntityManager.AddComponentData(newGoalEntity,
                    new AgentGoal{Agent = agentEntity});
            }
            EntityManager.AddComponentData(newGoalEntity, new Goal
            {
                GoalEntity = newGoalEntity,
                State = goalState,
                Priority = priority,
                CreateTime = time
            });
            EntityManager.AddComponentData(newGoalEntity, new IdleGoal
            {
                Time = (float)time
            });
        }
    }
}