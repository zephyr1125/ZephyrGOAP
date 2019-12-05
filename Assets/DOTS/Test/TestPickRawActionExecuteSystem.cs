using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.GameData.ComponentData;
using DOTS.Struct;
using DOTS.System;
using DOTS.System.ActionExecuteSystem;
using NUnit.Framework;
using Unity.Entities;

namespace DOTS.Test
{
    public class TestPickRawActionExecuteSystem : TestBase
    {
        private PickRawActionExecuteSystem _system;
        private Entity _agentEntity, _containerEntity, _currentStateEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<PickRawActionExecuteSystem>();

            _agentEntity = EntityManager.CreateEntity();
            _containerEntity = EntityManager.CreateEntity();
            _currentStateEntity = EntityManager.CreateEntity();
            
            //container预先存好物品
            var itemBuffer = EntityManager.AddBuffer<ContainedItemRef>(_containerEntity);
            itemBuffer.Add(new ContainedItemRef
            {
                ItemName = new NativeString64("item"),
                ItemEntity = new Entity {Index = 99, Version = 9}
            });
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new ReadyToActing());
            EntityManager.AddComponentData(_agentEntity, new PickRawAction());
            EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            //agent必须带有已经规划好的任务列表
            var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
            bufferNodes.Add(new Node
            {
                Name = new NativeString64(nameof(PickRawAction)),
                PreconditionsBitmask = 1,
                EffectsBitmask = 1 << 1,
            });
            var bufferStates = EntityManager.AddBuffer<State>(_agentEntity);
            bufferStates.Add(new State
            {
                SubjectType = StateSubjectType.Closest,
                Target = Entity.Null,
                Trait = typeof(RawTrait),
                Value = new NativeString64("item"),
                IsPositive = true,
            });
            bufferStates.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = _agentEntity,
                Trait = typeof(RawTrait),
                Value = new NativeString64("item"),
                IsPositive = true
            });
            //currentState存好物品状态
            bufferStates = EntityManager.AddBuffer<State>(_currentStateEntity);
            bufferStates.Add(new State
            {
                SubjectType = StateSubjectType.Target,
                Target = _containerEntity,
                Trait = typeof(RawTrait),
                IsPositive = true,
                Value = new NativeString64("item")
            });
            CurrentStatesHelper.CurrentStatesEntity = _currentStateEntity;
        }

        [Test]
        public void TargetRemoveItem()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(0, itemBuffer.Length);
        }

        [Test]
        public void AgentGotItem()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
            Assert.AreEqual(1, itemBuffer.Length);
            Assert.AreEqual(new ContainedItemRef
            {
                ItemName = new NativeString64("item"), ItemEntity = new Entity{Index = 99, Version = 9}
            }, itemBuffer[0]);
        }

        [Test]
        public void ProgressGoOn()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();

            var agent = EntityManager.GetComponentData<Agent>(_agentEntity);
            Assert.AreEqual(new Agent{ExecutingNodeId = 1},agent);
            Assert.False(EntityManager.HasComponent<ReadyToActing>(_agentEntity));
            Assert.True(EntityManager.HasComponent<ReadyToNavigating>(_agentEntity));
        }
        
        [Test]
        public void HasTarget_UseTarget()
        {
            var bufferStates = EntityManager.GetBuffer<State>(_agentEntity);
            bufferStates[0] = new State
            {
                SubjectType = StateSubjectType.Target,
                Target = _containerEntity,
                Trait = typeof(RawTrait),
                Value = new NativeString64("item"),
                IsPositive = true,
            };
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            var itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_containerEntity);
            Assert.AreEqual(0, itemBuffer.Length);
            itemBuffer = EntityManager.GetBuffer<ContainedItemRef>(_agentEntity);
            Assert.AreEqual(1, itemBuffer.Length);
            Assert.AreEqual(new ContainedItemRef
            {
                ItemName = new NativeString64("item"), ItemEntity = new Entity{Index = 99, Version = 9}
            }, itemBuffer[0]);
        }
    }
}