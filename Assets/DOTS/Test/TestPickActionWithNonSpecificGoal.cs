using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Struct;
using DOTS.System;
using DOTS.Test.Debugger;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Test
{
    public class TestPickActionWithNonSpecificGoal : TestBase
    {
        private GoalPlanningSystem _system;
        private Entity _agentEntity;

        private TestGoapDebugger _debugger;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            _debugger = new TestGoapDebugger();
            _system.Debugger = _debugger;
            
            _agentEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait)
            });
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
            //给CurrentStates写入假环境数据：世界里有2种食物来源
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(new State
            {
                Target = new Entity{Index = 8, Version = 1},
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
                ValueString = new NativeString64("peach")
            });
            buffer.Add(new State
            {
                Target = new Entity{Index = 9, Version = 1},
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
                ValueString = new NativeString64("apple")
            });
        }
        
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _debugger.Dispose();
        }
        
        //在使用非特指goal时，要每种物品一个setting
        [Test]
        public void OneSettingPerItemName()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Debug.Log(_debugger.GoalNodeView);
        }
    }
}