using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Test.Debugger;

namespace Zephyr.GOAP.Test
{
    public class TestActionExecuteBase : TestBase
    {
        protected Entity _agentEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _agentEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddBuffer<FailedPlanLog>(_agentEntity);
            EntityManager.AddComponentData(_agentEntity, new Translation());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());
            EntityManager.AddComponentData(_agentEntity, new ReadyToAct());
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
        }
    }
}