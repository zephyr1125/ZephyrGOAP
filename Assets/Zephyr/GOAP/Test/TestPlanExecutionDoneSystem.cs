using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Test
{
    public class TestPlanExecutionDoneSystem : TestBase
    {
        private PlanExecutionDoneSystem _system;
        private Entity _agentEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<PlanExecutionDoneSystem>();
            _agentEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 2});
            EntityManager.AddComponentData(_agentEntity, new ReadyToNavigating());
            var nodeBuffer = EntityManager.AddBuffer<Node>(_agentEntity);
            nodeBuffer.Add(new Node());
            nodeBuffer.Add(new Node());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State());
        }

        [Test]
        public void ClearNodesAndStates()
        {
            var nodeBuffer = EntityManager.GetBuffer<Node>(_agentEntity);
            Assert.AreEqual(2, nodeBuffer.Length);

            var stateBuffer = EntityManager.GetBuffer<State>(_agentEntity);
            Assert.AreEqual(1, stateBuffer.Length);
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.HasComponent<Node>(_agentEntity));
            stateBuffer = EntityManager.GetBuffer<State>(_agentEntity);
            Assert.AreEqual(0, stateBuffer.Length);
        }

        [Test]
        public void NextAgentState()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<NoGoal>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigating>(_agentEntity));
            
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }

        [Test]
        public void NotFinish_NoExecute()
        {
            EntityManager.SetComponentData(_agentEntity, new Agent{ExecutingNodeId = 1});
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var nodeBuffer = EntityManager.GetBuffer<Node>(_agentEntity);
            Assert.AreEqual(2, nodeBuffer.Length);

            var stateBuffer = EntityManager.GetBuffer<State>(_agentEntity);
            Assert.AreEqual(1, stateBuffer.Length);
            
            Assert.AreEqual(1, EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }
    }
}