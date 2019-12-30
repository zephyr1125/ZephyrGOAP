using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using DOTS.System;
using DOTS.System.SensorSystem;
using DOTS.Test.Debugger;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Test.ActionExpand
{
    public class TestPickDropSequence : TestBase
    {
        //一个原料，一个目标容器，一个agent，
        //实现 Pick -> Drop 的plan序列

        private GoalPlanningSystem _system;
        private Entity _itemSourceEntity, _targetContainerEntity, _agentEntity;

        private TestGoapDebugger _debugger;

        private State _goalState;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            _debugger = new TestGoapDebugger();
            _system.Debugger = _debugger;

            _itemSourceEntity = EntityManager.CreateEntity();
            _targetContainerEntity = EntityManager.CreateEntity();
            _agentEntity = EntityManager.CreateEntity();
            
            //游戏数据
            EntityManager.AddComponentData(_itemSourceEntity, new ItemContainer{IsTransferSource = true});
            var itemBuffer = EntityManager.AddBuffer<ContainedItemRef>(_itemSourceEntity);
            itemBuffer.Add(new ContainedItemRef {ItemName = new NativeString64("item")});
            EntityManager.AddComponentData(_targetContainerEntity, new ItemContainer{IsTransferSource = false});
            
            //GOAP数据
            EntityManager.AddComponentData(_itemSourceEntity, new ItemContainerTrait());
            EntityManager.AddComponentData(_agentEntity, new Agent());
            EntityManager.AddComponentData(_agentEntity, new PickItemAction());
            EntityManager.AddComponentData(_agentEntity, new DropItemAction());
            EntityManager.AddComponentData(_agentEntity, new GoalPlanning());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            _goalState = new State
            {
                Target = _targetContainerEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("item"),
            };
            stateBuffer.Add(_goalState);
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
            //SensorGroup喂入CurrentStates数据
            var sensor = World.GetOrCreateSystem<ItemSourceSensorSystem>();
            sensor.Update();
            sensor.ECBufferSystem.Update();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _debugger.Dispose();
        }

        [Test]
        public void ExpandCorrectNodeGraph()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var goalNodeView = _debugger.GoalNodeView;
            //goal正确
            Assert.AreEqual(1, goalNodeView.States.Length);
            Assert.AreEqual(_goalState, goalNodeView.States[0]);
            Assert.AreEqual(new State
            {
                Target = _targetContainerEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("item"),
            }, goalNodeView.States[0]);
            
            //Drop接Goal
            var dropNodeView = _debugger.GoalNodeView.Children[0];
            Assert.AreEqual(nameof(DropItemAction), dropNodeView.Name);
            Assert.AreEqual(1, dropNodeView.States.Length);
            Assert.AreEqual(new State
            {
                Target = _agentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("item"),
            }, dropNodeView.States[0]);
            
            //Pick接Drop
            var pickNodeView = dropNodeView.Children[0];
            Assert.AreEqual(nameof(PickItemAction), pickNodeView.Name);
            Assert.AreEqual(1, pickNodeView.States.Length);
            Assert.AreEqual(new State
            {
                Target = _itemSourceEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = new NativeString64("item"),
            }, pickNodeView.States[0]);
            
            //start接pick
            var startNodeView = pickNodeView.Children[0];
            Assert.AreEqual("start", startNodeView.Name);
            Assert.Zero(startNodeView.States.Length);
        }

        [Test]
        public void GetPath()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var pathResult = _debugger.PathResult;
            Debug.Log(pathResult);
        }
        
        [Test]
        public void SavePath()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var buffer = EntityManager.GetBuffer<Node>(_agentEntity);
            Assert.AreEqual(3, buffer.Length);
        }

        [Test]
        public void SavePathStates()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();

            var bufferNodes = EntityManager.GetBuffer<Node>(_agentEntity);
            var bufferStates = EntityManager.GetBuffer<State>(_agentEntity);
            
            //1 goal state + 2 precondition + 2 effect
            Assert.AreEqual(5, bufferStates.Length);
            
            //no states for goal node
            Assert.Zero(bufferNodes[0].PreconditionsBitmask);
            Assert.Zero(bufferNodes[0].EffectsBitmask);
            
            //1 is drop, 2 is pick
            var nodeDrop = bufferNodes[1];
            var nodePick = bufferNodes[2];
            for (var i = 0; i < bufferStates.Length; i++)
            {
                //nodeDrop应该只有1个precondition
                if ((nodeDrop.PreconditionsBitmask & (ulong)1 << i) > 0)
                {
                    Assert.AreEqual((ulong)1 << i, nodeDrop.PreconditionsBitmask);
                    Assert.AreEqual(new State
                    {
                        Target = _agentEntity,
                        Trait = typeof(ItemContainerTrait),
                        ValueString = new NativeString64("item"),
                    }, bufferStates[i]);
                }
                //和一个effect
                if ((nodeDrop.EffectsBitmask & (ulong)1 << i) > 0)
                {
                    Assert.AreEqual((ulong)1 << i, nodeDrop.EffectsBitmask);
                    Assert.AreEqual(new State
                    {
                        Target = _targetContainerEntity,
                        Trait = typeof(ItemContainerTrait),
                        ValueString = new NativeString64("item"),
                    }, bufferStates[i]);
                }
                //nodePick应该只有1个precondition
                if ((nodePick.PreconditionsBitmask & (ulong)1 << i) > 0)
                {
                    Assert.AreEqual((ulong)1 << i, nodePick.PreconditionsBitmask);
                    Assert.AreEqual(new State
                    {
                        Target = _itemSourceEntity,
                        Trait = typeof(ItemContainerTrait),
                        ValueString = new NativeString64("item"),
                    }, bufferStates[i]);
                }
                //和一个effect
                if ((nodePick.EffectsBitmask & (ulong)1 << i) > 0)
                {
                    Assert.AreEqual((ulong)1 << i, nodePick.EffectsBitmask);
                    Assert.AreEqual(new State
                    {
                        Target = _agentEntity,
                        Trait = typeof(ItemContainerTrait),
                        ValueString = new NativeString64("item"),
                    }, bufferStates[i]);
                }
            }
            
        }

        [Test]
        public void AlreadyHasResult_NotRun()
        {
            var buffer = EntityManager.AddBuffer<Node>(_agentEntity);
            buffer.Add(new Node {Name = new NativeString64("exist")});
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            buffer = EntityManager.GetBuffer<Node>(_agentEntity);
            Assert.AreEqual(1, buffer.Length);
        }
    }
}