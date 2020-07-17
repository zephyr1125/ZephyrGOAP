using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Sample.GoapImplement.System;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;
using Zephyr.GOAP.Tests.Debugger;

namespace Zephyr.GOAP.Sample.Tests
{
    public class TestActionExpandBase : TestBase
    {
        protected GoalPlanningSystemBase _system;
        protected Entity _agentEntity, _goalEntity;

        protected TestGoapDebugger _debugger;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _debugger = new TestGoapDebugger(); 
            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            _system.Debugger = _debugger;
            
            _agentEntity = EntityManager.CreateEntity();
            _goalEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddComponentData(_agentEntity, new Translation());
            EntityManager.AddComponentData(_agentEntity, new MaxMoveSpeed{value = 1});
            EntityManager.AddComponentData(_agentEntity, new Idle());

            World.GetOrCreateSystem<BaseStatesHelper>().Update();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _debugger.Dispose();
        }

        protected void SetGoal(State require,
         Priority priority = Priority.Normal, double time = 0)
        {
            EntityManager.AddComponentData(_goalEntity, new Goal
            {
                GoalEntity = _goalEntity,
                Require = require,
                Priority = priority,
                CreateTime = time
            });
            EntityManager.AddComponentData(_goalEntity, new IdleGoal
            {
                Time = (float)time
            });
        }

        protected Goal GetGoal()
        {
            return EntityManager.GetComponentData<Goal>(_goalEntity);
        }
    }
}