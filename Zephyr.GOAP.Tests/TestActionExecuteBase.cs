using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Tests
{
    public class TestActionExecuteBase : TestBase
    {
        protected Entity _agentEntity, _actionNodeEntity;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _agentEntity = EntityManager.CreateEntity();
            _actionNodeEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddComponentData(_agentEntity, new Translation());
            EntityManager.AddComponentData(_agentEntity, new ReadyToAct());
            
            EntityManager.AddComponentData(_actionNodeEntity, new ActionNodeActing());
        }
    }
}