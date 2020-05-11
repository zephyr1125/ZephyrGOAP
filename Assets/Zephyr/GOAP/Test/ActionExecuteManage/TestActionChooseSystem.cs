using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System.ActionExecuteManage;

namespace Zephyr.GOAP.Test.ActionExecuteManage
{
    public class TestActionChooseSystem : TestActionExecuteBase
    {
        private ActionChooseSystem _system;
        
        private Entity _executingNodeEntity, _hasDependencyNodeEntity,
            _availableNodeEntityOlder, _availableNodeEntityNewer;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<ActionChooseSystem>();
            
            Utils.NextAgentState<ReadyToAct, Idle>(_agentEntity, EntityManager);

            _executingNodeEntity = EntityManager.CreateEntity();
            _hasDependencyNodeEntity = EntityManager.CreateEntity();
            _availableNodeEntityOlder = EntityManager.CreateEntity();
            _availableNodeEntityNewer = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_executingNodeEntity, new Node());
            EntityManager.AddComponentData(_executingNodeEntity, new ExecutingNode());
            
            EntityManager.AddComponentData(_hasDependencyNodeEntity, new Node());
            EntityManager.AddBuffer<NodeDependency>(_hasDependencyNodeEntity);
            
            EntityManager.AddComponentData(_availableNodeEntityOlder, new Node
            {
                AgentExecutorEntity = _agentEntity,
                EstimateStartTime = 0,
            });
            
            EntityManager.AddComponentData(_availableNodeEntityNewer, new Node
            {
                AgentExecutorEntity = _agentEntity,
                EstimateStartTime = 1,
            });
        }

        [Test]
        public void ChooseCorrectAction()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();

            Assert.AreEqual(_availableNodeEntityOlder, 
                EntityManager.GetComponentData<ExecutingAgent>(_agentEntity).NodeEntity);
            
            Assert.AreEqual(_agentEntity,
                EntityManager.GetComponentData<ExecutingNode>(_availableNodeEntityOlder).AgentEntity);
            
            Assert.AreNotEqual(_agentEntity,
                EntityManager.GetComponentData<ExecutingNode>(_executingNodeEntity).AgentEntity);
            Assert.IsFalse(EntityManager.HasComponent<ExecutingNode>(_hasDependencyNodeEntity));
            Assert.IsFalse(EntityManager.HasComponent<ExecutingNode>(_availableNodeEntityNewer));
        }
    }
}