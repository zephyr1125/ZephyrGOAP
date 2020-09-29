using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System;
using Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem;
using Zephyr.GOAP.System;
using Zephyr.GOAP.Tests;

namespace Zephyr.GOAP.Sample.Tests.ActionExpand
{
    /// <summary>
    /// BUG: 原料不足时出现栈溢出
    /// </summary>
    public class TestNoEnoughItemInWorld : TestActionExpandBase<GoalPlanningSystem>
    {
        private Entity _collector0Entity, _rawAppleEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _collector0Entity = EntityManager.CreateEntity();
            _rawAppleEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            EntityManager.AddComponentData(_agentEntity, new CollectAction{Level = 1});
            EntityManager.AddComponentData(_agentEntity, new PickRawAction());
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());
            EntityManager.AddComponentData(_agentEntity, new AgentMoveSpeed{value = 1});
            
            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<ItemTransferTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 1
            });
            
            //给BaseStates写入假环境数据：世界里有原料、配方
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);

            buffer.Add(new State
            {
                Target = _collector0Entity,
                Position = new float3(4, 0, 0),
                Trait = TypeManager.GetTypeIndex<CollectorTrait>(),
            });
            buffer.Add(new State
            {
                Target = _collector0Entity,
                Position = new float3(4, 0, 0),
                Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 0
            });
            
            buffer.Add(new State
            {
                Target = _rawAppleEntity,
                Position = new float3(5,0,0),
                Trait = TypeManager.GetTypeIndex<RawSourceTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 0
            });
        }

        [Test]
        public void PlanFailWithNoError()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(_debugger.IsPlanSuccess());
        }

    }
}