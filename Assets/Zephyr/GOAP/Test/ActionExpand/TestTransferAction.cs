using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Logger;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;
using Zephyr.GOAP.System.SensorSystem;

namespace Zephyr.GOAP.Test.ActionExpand
{
    /// <summary>
    /// 目标：获得物品
    /// 预期：规划出Transfer
    /// </summary>
    public class TestTransferAction : TestActionExpandBase
    {
        private Entity _itemSourceEntity, _itemDestinationEntity;
        private State _currentState;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _itemSourceEntity = EntityManager.CreateEntity();
            _itemDestinationEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new TransferAction());
            //goal
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            stateBuffer.Add(new State
            {
                Target = _itemDestinationEntity,
                Trait = typeof(ItemDestinationTrait),
                ValueString = "raw_apple"
            });
            
            //给CurrentStates写入假环境数据：源头有物品
            _currentState = new State
            {
                Target = _itemSourceEntity,
                Trait = typeof(ItemSourceTrait),
                ValueString = "raw_apple",
            };
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.Add(_currentState);
        }

        [Test]
        public void PlanTransfer()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(nameof(TransferAction), pathResult[1].Name);
            Assert.AreEqual("+[3,1](ItemSourceTrait)raw_apple",
                pathResult[1].States[0].ToString());
        }

        [Test]
        public void PlanTransferForScopeGoal()
        {
            var buffer = EntityManager.GetBuffer<State>(_agentEntity);
            var goalState = buffer[0];
            goalState.ValueString = new NativeString64();
            goalState.ValueTrait = typeof(FoodTrait);
            buffer[0] = goalState;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            var pathResult = _debugger.PathResult;
            Assert.AreEqual(nameof(TransferAction), pathResult[1].Name);
            Assert.AreEqual("+[3,1](ItemSourceTrait)raw_apple",
                pathResult[1].States[0].ToString());
        }

        [Test]
        public void NoItemSourceInWorld_PlanFail()
        {
            var buffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            buffer.RemoveAt(0);
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<NoGoal>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<GoalPlanning>(_agentEntity));
            Assert.Zero(EntityManager.GetBuffer<State>(_agentEntity).Length);
        }
        
        //多重setting产生多个node
        [Test]
        public void MultiSettingToMultiNodes()
        {
            var buffer = EntityManager.GetBuffer<State>(_agentEntity);
            var goalState = buffer[0];
            goalState.ValueString = new NativeString64();
            goalState.ValueTrait = typeof(FoodTrait);
            buffer[0] = goalState;
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            //4种符合FoodTrait的node
            Assert.AreEqual(4, _debugger.GoalNodeView.Children.Count);
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals("raw_apple"))));
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals("raw_peach"))));
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals("roast_apple"))));
            Assert.IsTrue(_debugger.GoalNodeView.Children.Any(nodeView => nodeView.States.Any(
                state => state.ValueString.Equals("roast_peach"))));
        }
    }
}