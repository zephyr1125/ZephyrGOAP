using DOTS.Component;
using DOTS.Component.Trait;
using DOTS.Test.System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Test
{
    public class TestActionScheduler : TestBase
    {
        private TestActionSchedulerSystem _system;

        private Entity _goalNode, _containerEntity, _agentEntity;
        private NativeList<Entity> _unexpandedNodes;
        private StackData _stackData;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _goalNode = EntityManager.CreateEntity();
            _containerEntity = EntityManager.CreateEntity();
            _agentEntity = EntityManager.CreateEntity();
            
            _unexpandedNodes = new NativeList<Entity>(Allocator.Persistent);

            _stackData = new StackData
            {
                AgentEntity = _agentEntity,
                CurrentStates = new StateGroup(1, Allocator.Persistent)
                {
                    new State
                    {
                        Target = _agentEntity,
                        Trait = typeof(Inventory),
                        Value = new NativeString64("test")
                    }
                }
            };
            EntityManager.AddComponentData(_goalNode, new Node());
            var buffer = EntityManager.AddBuffer<State>(_goalNode);
            buffer.Add(new State
            {
                Target = _containerEntity,
                Trait = typeof(Inventory),
                Value = new NativeString64("test"),
            });
            
            _unexpandedNodes.Add(_goalNode);

            _system = World.GetOrCreateSystem<TestActionSchedulerSystem>();
            _system.UnexpandedNodes = _unexpandedNodes;
            _system.StackData = _stackData;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _unexpandedNodes.Dispose();
            _stackData.Dispose();
        }

        [Test]
        public void CreateNode()
        {
            _system.Update();
            _system.ECBufferSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var nodeQuery = EntityManager.CreateEntityQuery(typeof(Node));
            var nodeEntities = nodeQuery.ToEntityArray(Allocator.TempJob);
            var nodes = nodeQuery.ToComponentDataArray<Node>(Allocator.TempJob);
            Assert.AreEqual(2, nodeEntities.Length);
            Assert.AreEqual(_goalNode, nodes[1].parent);
            var buffer = EntityManager.GetBuffer<State>(nodeEntities[1]);
            var state = buffer[0];
            Assert.AreEqual(new State
            {
                Target = _agentEntity,
                Trait = typeof(Inventory),
                Value = new NativeString64("test")
            }, state);
            nodeEntities.Dispose();
            nodes.Dispose();
        }

        [Test]
        public void NoInventoryGoal_NoNode()
        {
            var buffer = EntityManager.GetBuffer<State>(_goalNode);
            buffer[0] = new State
            {
                Target = _containerEntity,
                Trait = typeof(GatherStation),
                Value = new NativeString64("test"),
            };
            
            _system.Update();
            _system.ECBufferSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var nodeQuery = EntityManager.CreateEntityQuery(typeof(Node));
            var nodeEntities = nodeQuery.ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(1, nodeEntities.Length);
            nodeEntities.Dispose();
        }
    }
}