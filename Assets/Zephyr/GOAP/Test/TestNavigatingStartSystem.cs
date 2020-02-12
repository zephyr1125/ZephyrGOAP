using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Entity = Unity.Entities.Entity;

namespace Zephyr.GOAP.Test
{
    public class TestNavigatingStartSystem : TestBase
    {
        private NavigatingStartSystem _system;
        private Entity _agentEntity, _containerEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<NavigatingStartSystem>();

            _agentEntity = EntityManager.CreateEntity();
            _containerEntity = EntityManager.CreateEntity();
            
            //container要有位置数据
            EntityManager.AddComponentData(_containerEntity, new Translation{Value = new float3(9,0,0)});
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new ReadyToNavigate());
            EntityManager.AddComponentData(_agentEntity, new Translation{Value = float3.zero});
            //agent必须带有已经规划好的任务列表
            var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
            bufferNodes.Add(new Node
            {
                NavigatingSubject = _containerEntity,
            });
        }
        
        //为agent赋予移动目标
        [Test]
        public void AddTargetPositionToAgent()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(new float3(9,0,0),
                EntityManager.GetComponentData<TargetPosition>(_agentEntity).Value);
        }
        
        //切换agent状态
        [Test]
        public void NextAgentState()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<Navigating>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigate>(_agentEntity));
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }
        
        //目标为自身则直接结束
        [Test]
        public void TargetIsSelf_ToNextState()
        {
            var buffer = EntityManager.GetBuffer<Node>(_agentEntity);
            buffer[0] = new Node
            {
                NavigatingSubject = _agentEntity,
            };
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigate>(_agentEntity));
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }
        
        //目标为空则直接结束
        [Test]
        public void TargetIsNull_ToNextState()
        {
            var buffer = EntityManager.GetBuffer<Node>(_agentEntity);
            buffer[0] = new Node
            {
                NavigatingSubject = Entity.Null,
            };
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigate>(_agentEntity));
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }
    }
}