using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Struct;
using DOTS.System;
using DOTS.Test.Debugger;
using NUnit.Framework;
using Unity.Entities;

namespace DOTS.Test
{
    public class TestDropActionWithScopeGoal : TestBase
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
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = new Entity {Index = 9, Version = 9},
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait)
            });
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
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
            
            Assert.AreEqual(2, _debugger.GoalNodeView.Children.Count);
            Assert.AreEqual(new NativeString64("peach"),
                _debugger.GoalNodeView.Children[1].States[0].ValueString);
            Assert.AreEqual(new NativeString64("apple"),
                _debugger.GoalNodeView.Children[0].States[0].ValueString);
        }
    }
}