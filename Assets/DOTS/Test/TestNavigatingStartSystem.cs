using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using DOTS.System;
using DOTS.System.ActionExecuteSystem;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Entity = Unity.Entities.Entity;

namespace DOTS.Test
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
            EntityManager.AddComponentData(_agentEntity, new ReadyToActing());
            EntityManager.AddComponentData(_agentEntity, new PickRawAction());
            EntityManager.AddComponentData(_agentEntity, new Translation{Value = float3.zero});
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
                SubjectType = StateSubjectType.Target,
                Target = _containerEntity,
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
        }
    }
}