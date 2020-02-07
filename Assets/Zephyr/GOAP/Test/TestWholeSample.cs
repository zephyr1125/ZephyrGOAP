using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Test
{
    public class TestWholeSample : TestBase
    {
        private GoalPlanningSystem _system;
        private Entity _agent;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            _agent = EntityManager.CreateEntity();

            EntityManager.AddComponentData(_agent, new Agent());
            
            EntityManager.AddComponentData(_agent, new EatAction());
            EntityManager.AddComponentData(_agent, new CookAction());
            EntityManager.AddComponentData(_agent, new PickItemAction());
            EntityManager.AddComponentData(_agent, new DropItemAction());

            EntityManager.AddComponentData(_agent, new GoalPlanning());
            
            //goal
            var stateBuffer = EntityManager.AddBuffer<State>(_agent);
            stateBuffer.Add(new State
            {
                Target = _agent,
                Trait = typeof(StaminaTrait),
            });
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
        }
    }
}