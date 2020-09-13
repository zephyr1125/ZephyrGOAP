using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.System.ActionExecuteManage;
using Assert = Unity.Assertions.Assert;

namespace Zephyr.GOAP.Tests.ActionExecuteManage
{
    public class TestActionExecuteDoneSystem : TestBase
    {
        private ActionExecuteDoneSystem _system;

        private Entity doneNodeEntity0, doneNodeEntity1, notDoneNodeEntity;
        private Entity depend1NodeEntity, depend2NodeEntity;
        private Entity doneAgentEntity0, doneAgentEntity1, notDoneAgentEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<ActionExecuteDoneSystem>();

            doneNodeEntity0 = EntityManager.CreateEntity();
            doneNodeEntity1 = EntityManager.CreateEntity();
            notDoneNodeEntity = EntityManager.CreateEntity();

            depend1NodeEntity = EntityManager.CreateEntity();
            depend2NodeEntity = EntityManager.CreateEntity();
            
            doneAgentEntity0 = EntityManager.CreateEntity();
            doneAgentEntity1 = EntityManager.CreateEntity();
            notDoneAgentEntity = EntityManager.CreateEntity();

            //nodes
            EntityManager.AddComponentData(doneNodeEntity0,
                new Node {AgentExecutorEntity = doneAgentEntity0});
            EntityManager.AddComponentData(doneNodeEntity0, new ActionNodeDone());
            
            EntityManager.AddComponentData(doneNodeEntity1,
                new Node {AgentExecutorEntity = doneAgentEntity1});
            EntityManager.AddComponentData(doneNodeEntity1, new ActionNodeDone());
            
            EntityManager.AddComponentData(notDoneNodeEntity,
                new Node {AgentExecutorEntity = notDoneAgentEntity});
            EntityManager.AddComponentData(notDoneNodeEntity, new ActionNodeActing());
            
            //dependency
            var buffer = EntityManager.AddBuffer<NodeDependency>(depend1NodeEntity);
            buffer.Add(new NodeDependency {Entity = doneNodeEntity0});

            buffer = EntityManager.AddBuffer<NodeDependency>(depend2NodeEntity);
            buffer.Add(new NodeDependency {Entity = doneNodeEntity1});
            buffer.Add(new NodeDependency {Entity = notDoneNodeEntity});
            
            //agents
            EntityManager.AddComponentData(doneAgentEntity0, new ActDone());
            EntityManager.AddComponentData(doneAgentEntity1, new ActDone());
            EntityManager.AddComponentData(notDoneAgentEntity, new Acting());
        }

        [Test]
        public void CorrectClean()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.Exists(doneNodeEntity0));
            Assert.IsFalse(EntityManager.Exists(doneNodeEntity1));
            Assert.IsTrue(EntityManager.Exists(notDoneNodeEntity));
            
            Assert.IsFalse(EntityManager.HasComponent<NodeDependency>(depend1NodeEntity));
            Assert.IsTrue(EntityManager.HasComponent<NodeDependency>(depend2NodeEntity));
            var buffer = EntityManager.GetBuffer<NodeDependency>(depend2NodeEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(notDoneNodeEntity, buffer[0].Entity);
            
            Assert.IsFalse(EntityManager.HasComponent<ActDone>(doneAgentEntity0));
            Assert.IsTrue(EntityManager.HasComponent<Idle>(doneAgentEntity0));
            Assert.IsFalse(EntityManager.HasComponent<ActDone>(doneAgentEntity1));
            Assert.IsTrue(EntityManager.HasComponent<Idle>(doneAgentEntity1));
            Assert.IsFalse(EntityManager.HasComponent<Idle>(notDoneAgentEntity));
            Assert.IsTrue(EntityManager.HasComponent<Acting>(notDoneAgentEntity));
        }

        [Test]
        public void Clean_DeltaStates()
        {
            var deltaStatesEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(deltaStatesEntity,
                new DeltaStates{ActionNodeEntity = doneNodeEntity0});
            
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.Exists(deltaStatesEntity));
        }
    }
}