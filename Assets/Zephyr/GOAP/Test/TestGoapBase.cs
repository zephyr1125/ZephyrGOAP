using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Test.Debugger;

namespace Zephyr.GOAP.Test
{
    public class TestGoapBase : TestBase
    {
        protected GoalPlanningSystem _system;
        protected Entity _agentEntity;

        protected TestGoapDebugger _debugger;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            _debugger = new TestGoapDebugger();
            _system.Debugger = _debugger;
            
            _agentEntity = EntityManager.CreateEntity();
            EntityManager.SetName(_agentEntity, "agent");
            
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
    }
}