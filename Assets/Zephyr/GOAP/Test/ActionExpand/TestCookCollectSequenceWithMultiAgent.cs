using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.SensorSystem;

namespace Zephyr.GOAP.Test.ActionExpand
{
    /// <summary>
    /// 目标：获得体力
    /// 预期：规划出ECook-Sequence序列
    /// 并且需要多人合作实现
    /// </summary>
    public class TestCookCollectSequenceWithMultiAgent : TestActionExpandBase
    {
        private Entity _cookerEntity, _diningTableEntity, _collectorEntity, _rawAppleEntity, _rawPeachEntity;
        private Entity _agentBEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _agentBEntity = EntityManager.CreateEntity();
            _cookerEntity = EntityManager.CreateEntity();
            _diningTableEntity = EntityManager.CreateEntity();
            _collectorEntity = EntityManager.CreateEntity();
            _rawAppleEntity = EntityManager.CreateEntity();
            _rawPeachEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new CookAction{Level = 1});
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            EntityManager.AddComponentData(_agentEntity, new CollectAction{Level = 1});
            EntityManager.AddComponentData(_agentEntity, new PickRawAction());
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());
            EntityManager.AddComponentData(_agentEntity, new MaxMoveSpeed{value = 1});
            
            EntityManager.AddComponentData(_agentBEntity, new Agent());
            EntityManager.AddComponentData(_agentBEntity, new Translation());
            EntityManager.AddComponentData(_agentBEntity, new CookAction{Level = 0});
            EntityManager.AddComponentData(_agentBEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentBEntity, new DropItemAction());
            EntityManager.AddComponentData(_agentBEntity, new CollectAction{Level = 0});
            EntityManager.AddComponentData(_agentBEntity, new PickRawAction());
            EntityManager.AddComponentData(_agentBEntity, new DropRawAction());
            EntityManager.AddComponentData(_agentBEntity, new MaxMoveSpeed{value = 2});
            
            SetGoal(new State
            {
                Target = _cookerEntity,
                Position = new float3(2, 0, 0),
                Trait = typeof(ItemSourceTrait),
                ValueString = "feast"
            });
            
            //给CurrentStates写入假环境数据：世界里有原料、配方
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Position = new float3(2, 0, 0),
                Trait = typeof(CookerTrait),
            });
            buffer.Add(new State
            {
                Target = _collectorEntity,
                Position = new float3(4, 0, 0),
                Trait = typeof(CollectorTrait),
            });
            buffer.Add(new State
            {
                Target = _collectorEntity,
                Position = new float3(4, 0, 0),
                Trait = typeof(ItemPotentialSourceTrait),
                ValueString = "raw_apple"
            });
            buffer.Add(new State
            {
                Target = _collectorEntity,
                Position = new float3(4, 0, 0),
                Trait = typeof(ItemPotentialSourceTrait),
                ValueString = "raw_peach"
            });
            buffer.Add(new State
            {
                Target = _rawAppleEntity,
                Position = new float3(5,0,0),
                Trait = typeof(RawSourceTrait),
                ValueString = "raw_apple"
            });
            buffer.Add(new State
            {
                Target = _rawPeachEntity,
                Position = new float3(6,0,0),
                Trait = typeof(RawSourceTrait),
                ValueString = "raw_peach"
            });
            
            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }

        [Test]
        public void PlanCookCollect()
        {
            Profiler.logFile = nameof(TestCookCollectSequenceWithMultiAgent);
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            
            Profiler.BeginSample(nameof(TestCookCollectSequenceWithMultiAgent));
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            Profiler.EndSample();

            Profiler.enabled = false;
            Profiler.logFile = "";
            
            Debug.Log(_debugger.GoalNodeLog);
            // var pathResult = _debugger.PathResult;
        }
    }
}