using DOTS.ActionJob;
using DOTS.Component;
using DOTS.Component.Actions;
using DOTS.Component.Trait;
using DOTS.Debugger;
using DOTS.GameData.ComponentData;
using DOTS.Struct;
using DOTS.System;
using DOTS.System.SensorSystem;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Test
{
    public class TestPickDropSequence : TestBase
    {
        //一个原料，一个目标容器，一个agent，
        //实现 Pick -> Drop 的plan序列

        private GoalPlanningSystem _system;
        private Entity _rawSourceEntity, _targetContainerEntity, _agentEntity;

        private TestGoapDebugger _debugger;

        private State _goalState;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<GoalPlanningSystem>();
            _debugger = new TestGoapDebugger();
            _system.Debugger = _debugger;

            _rawSourceEntity = EntityManager.CreateEntity();
            _targetContainerEntity = EntityManager.CreateEntity();
            _agentEntity = EntityManager.CreateEntity();
            
            //游戏数据
            EntityManager.AddComponentData(_rawSourceEntity, new ItemContainer{IsTransferSource = true});
            var itemBuffer = EntityManager.AddBuffer<ContainedItemRef>(_rawSourceEntity);
            itemBuffer.Add(new ContainedItemRef {ItemName = new NativeString64("item")});
            EntityManager.AddComponentData(_targetContainerEntity, new ItemContainer{IsTransferSource = false});
            
            //GOAP数据
            EntityManager.AddComponentData(_rawSourceEntity, new RawTrait());
            EntityManager.AddComponentData(_agentEntity, new Agent());
            var actionBuffer = EntityManager.AddBuffer<Action>(_agentEntity);
            actionBuffer.Add(new Action {ActionName = new NativeString64(nameof(PickRawActionJob))});
            actionBuffer.Add(new Action {ActionName = new NativeString64(nameof(DropRawActionJob))});
            EntityManager.AddComponentData(_agentEntity, new PlanningGoal());
            var stateBuffer = EntityManager.AddBuffer<State>(_agentEntity);
            _goalState = new State
            {
                SubjectType = StateSubjectType.Target,
                Target = _targetContainerEntity,
                Trait = typeof(RawTrait),
                Value = new NativeString64("item"),
                IsPositive = true
            };
            stateBuffer.Add(_goalState);
            
            World.GetOrCreateSystem<CurrentStatesHelper>().Update();
            //SensorGroup喂入CurrentStates数据
            var sensor = World.GetOrCreateSystem<RawSourceSensorSystem>();
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

            var nodeGraph = _debugger.NodeGraph;
            //goal正确
            var goalStates = nodeGraph.GetStateGroup(nodeGraph.GetGoalNode(), Allocator.Temp);
            Assert.AreEqual(1, goalStates.Length());
            Assert.AreEqual(_goalState, goalStates[0]);
            goalStates.Dispose();
            
            //start正确
            var startNode = nodeGraph.GetStartNode();
            var startNodeStates = nodeGraph.GetStateGroup(startNode, Allocator.Temp);
            Assert.Zero(startNodeStates.Length());
            
            //start接pick
            var edges = nodeGraph.GetEdgeToParents(startNode);
            var edgesCount = 0;
            var pickNode = default(Node);
            while (edges.MoveNext())
            {
                edgesCount++;
                var edge = edges.Current;
                Assert.AreEqual(new NativeString64("start"),
                    edge.ActionName);
                pickNode = edge.Parent;
                var parentStates = nodeGraph.GetStateGroup(pickNode, Allocator.Temp);
                Assert.AreEqual(1, parentStates.Length());
                Assert.AreEqual(new State
                {
                    SubjectType = StateSubjectType.Closest,
                    Target = Entity.Null,
                    Trait = typeof(RawTrait),
                    Value = new NativeString64("item"),
                    IsPositive = true,
                }, parentStates[0]);
                parentStates.Dispose();
            }
            Assert.AreEqual(1, edgesCount);
            
            //Pick接Drop
            edges = nodeGraph.GetEdgeToParents(pickNode);
            edgesCount = 0;
            var dropNode = default(Node);
            while (edges.MoveNext())
            {
                edgesCount++;
                var edge = edges.Current;
                Assert.AreEqual(new NativeString64(nameof(PickRawActionJob)),
                    edge.ActionName);
                dropNode = edge.Parent;
                var parentStates = nodeGraph.GetStateGroup(dropNode, Allocator.Temp);
                Assert.AreEqual(1, parentStates.Length());
                Assert.AreEqual(new State
                {
                    SubjectType = StateSubjectType.Self,
                    Target = _agentEntity,
                    Trait = typeof(RawTrait),
                    Value = new NativeString64("item"),
                    IsPositive = true,
                }, parentStates[0]);
                parentStates.Dispose();
            }
            Assert.AreEqual(1, edgesCount);
            
            //Drop接Goal
            edges = nodeGraph.GetEdgeToParents(dropNode);
            edgesCount = 0;
            var goalNode = default(Node);
            while (edges.MoveNext())
            {
                edgesCount++;
                var edge = edges.Current;
                Assert.AreEqual(new NativeString64(nameof(DropRawActionJob)),
                    edge.ActionName);
                goalNode = edge.Parent;
                var parentStates = nodeGraph.GetStateGroup(goalNode, Allocator.Temp);
                Assert.AreEqual(1, parentStates.Length());
                Assert.AreEqual(new State
                {
                    SubjectType = StateSubjectType.Target,
                    Target = _targetContainerEntity,
                    Trait = typeof(RawTrait),
                    Value = new NativeString64("item"),
                    IsPositive = true,
                }, parentStates[0]);
                parentStates.Dispose();
            }
            Assert.AreEqual(1, edgesCount);
            
            startNodeStates.Dispose();
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
    }
}