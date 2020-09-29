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
    /// 目标：获得体力
    /// 预期：规划出ECook-Sequence序列
    /// 并且需要多人合作实现
    /// </summary>
    public class TestCookCollectSequenceWithMultiAgent : TestActionExpandBase<GoalPlanningSystem>
    {
        private Entity _cookerEntity, _diningTableEntity, _collector0Entity, _collector1Entity, _rawAppleEntity, _rawPeachEntity;
        private Entity _agentBEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _agentBEntity = EntityManager.CreateEntity();
            _cookerEntity = EntityManager.CreateEntity();
            _diningTableEntity = EntityManager.CreateEntity();
            _collector0Entity = EntityManager.CreateEntity();
            _collector1Entity = EntityManager.CreateEntity();
            _rawAppleEntity = EntityManager.CreateEntity();
            _rawPeachEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new EatAction());
            EntityManager.AddComponentData(_agentEntity, new CookAction{Level = 1});
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            EntityManager.AddComponentData(_agentEntity, new CollectAction{Level = 1});
            EntityManager.AddComponentData(_agentEntity, new PickRawAction());
            EntityManager.AddComponentData(_agentEntity, new DropRawAction());
            EntityManager.AddComponentData(_agentEntity, new AgentMoveSpeed{value = 1});
            
            EntityManager.AddComponentData(_agentBEntity, new Agent());
            EntityManager.AddComponentData(_agentBEntity, new Idle());
            EntityManager.AddComponentData(_agentBEntity, new Translation());
            EntityManager.AddComponentData(_agentBEntity, new EatAction());
            EntityManager.AddComponentData(_agentBEntity, new CookAction{Level = 0});
            EntityManager.AddComponentData(_agentBEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentBEntity, new DropItemAction());
            EntityManager.AddComponentData(_agentBEntity, new CollectAction{Level = 0});
            EntityManager.AddComponentData(_agentBEntity, new PickRawAction());
            EntityManager.AddComponentData(_agentBEntity, new DropRawAction());
            EntityManager.AddComponentData(_agentBEntity, new AgentMoveSpeed{value = 2});
            
            // SetGoal(new State
            // {
            //     Target = _cookerEntity,
            //     Position = new float3(2, 0, 0),
            //     Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
            //     ValueString = "feast"
            // });
            
            SetGoal(new State
            {
                Target = _agentEntity,
                Trait = TypeManager.GetTypeIndex<StaminaTrait>(),
            });
            
            //给BaseStates写入假环境数据：世界里有原料、配方
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            buffer.Add(new State
            {
                Target = _diningTableEntity,
                Position = new float3(1,0,0),
                Trait = TypeManager.GetTypeIndex<DiningTableTrait>(),
            });
            buffer.Add(new State
            {
                Target = _cookerEntity,
                Position = new float3(2, 0, 0),
                Trait = TypeManager.GetTypeIndex<CookerTrait>(),
            });
            
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
                Amount = 255
            });
            buffer.Add(new State
            {
                Target = _collector0Entity,
                Position = new float3(4, 0, 0),
                Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                ValueString = ItemNames.Instance().RawPeachName,
                Amount = 255
            });
            
            // buffer.Add(new State
            // {
            //     Target = _collector1Entity,
            //     Position = new float3(3, 0, 0),
            //     Trait = TypeManager.GetTypeIndex<CollectorTrait>(),
            // });
            // buffer.Add(new State
            // {
            //     Target = _collector1Entity,
            //     Position = new float3(3, 0, 0),
            //     Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
            //     ValueString = "raw_apple"
            // });
            // buffer.Add(new State
            // {
            //     Target = _collector1Entity,
            //     Position = new float3(3, 0, 0),
            //     Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
            //     ValueString = Sample.StringTable.Instance().RawPeachName
            // });
            
            buffer.Add(new State
            {
                Target = _rawAppleEntity,
                Position = new float3(5,0,0),
                Trait = TypeManager.GetTypeIndex<RawSourceTrait>(),
                ValueString = ItemNames.Instance().RawAppleName,
                Amount = 255
            });

            buffer.Add(new State
            {
                Target = _rawPeachEntity,
                Position = new float3(6,0,0),
                Trait = TypeManager.GetTypeIndex<RawSourceTrait>(),
                ValueString = ItemNames.Instance().RawPeachName,
                Amount = 255
            });

            var recipeSensorSystem = World.GetOrCreateSystem<RecipeSensorSystem>();
            recipeSensorSystem.Update();
        }

        [Test]
        [Repeat(1)]
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
            
            // Debug.Log(_debugger.GoalNodeLog);
            // var pathResult = _debugger.PathResult;
            
            Assert.IsTrue(_debugger.IsPlanSuccess());
        }
        
        
        [TestCase(1)]
        public void PerformanceTest(int times)
        {
            float totalTime = 0;
            
            for (var i = 0; i < times; i++)
            {
                SetUp();
                _system.Debugger.SetWriteFile(false);
                
                _system.Update();
                _system.ECBSystem.Update();
                EntityManager.CompleteAllJobs();
                //第一次不算，因为有额外的编译时间
                var time = float.Parse(_debugger.GetLog().results[0].timeCost);
                if(i>0)totalTime += time;

                TearDown();
            }

            var averageTime = totalTime / (times-1);
            Debug.Log($"[Performance Test]Average Time = {averageTime}");
        }

    }
}