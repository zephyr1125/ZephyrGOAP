using DOTS.Component;
using DOTS.Component.Trait;
using DOTS.Struct;
using DOTS.Test.System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Test
{
    public class TestActionScheduler : TestBase
    {
        private TestActionSchedulerSystem _system;

        private NodeGraphGroup _nodeGraphGroup;
        private Node _goalNode;
        private NativeList<Node> _unexpandedNodes;
        
        private Entity  _containerEntity, _agentEntity;
        private StackData _stackData;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            _containerEntity = EntityManager.CreateEntity();
            _agentEntity = EntityManager.CreateEntity();

            //node graph 初始创建， 只塞一个goal node
            _nodeGraphGroup = new NodeGraphGroup
            {
                Nodes = new NativeList<Node>(Allocator.Persistent),
                NodeToParent = new NativeHashMap<Node, Node>(1, Allocator.Persistent),
                NodeToChildren = new NativeMultiHashMap<Node, Node>(1, Allocator.Persistent),
                NodeStates = new NativeMultiHashMap<Node, State>(1, Allocator.Persistent)
            };
            _goalNode = new Node(default);
            _nodeGraphGroup.Nodes.Add(_goalNode);
            _nodeGraphGroup.NodeStates.Add(_goalNode, new State
            {
                Target = _containerEntity,
                Trait = typeof(Inventory),
                Value = new NativeString64("test"),
            });

            //未展开列表放入goal node
            _unexpandedNodes = new NativeList<Node>(Allocator.Persistent) {_goalNode};

            _stackData = new StackData
            {
                AgentEntity = _agentEntity,
                CurrentStates = new StateGroup(1, Allocator.Persistent)
                {
                    new State
                    {
                        Target = _agentEntity,
                        Trait = typeof(Inventory),
                        Value = new NativeString64("test")
                    }
                }
            };

            _system = World.GetOrCreateSystem<TestActionSchedulerSystem>();
            _system.UnexpandedNodes = _unexpandedNodes;
            _system.StackData = _stackData;
            _system.NodeGraphGroup = _nodeGraphGroup;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            _unexpandedNodes.Dispose();
            _stackData.Dispose();
            _nodeGraphGroup.Dispose();
        }

        [Test]
        public void CreateNode()
        {
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(2, _nodeGraphGroup.Nodes.Length);
            var newNode = _nodeGraphGroup.Nodes[1];
            Assert.AreEqual(_goalNode, _nodeGraphGroup.NodeToParent[newNode]);
            
            _nodeGraphGroup.NodeToChildren.TryGetFirstValue(_goalNode, out var child, out var it);
            Assert.AreEqual(newNode, child);

            _nodeGraphGroup.NodeStates.TryGetFirstValue(child, out var childState, out it);
            Assert.AreEqual(new State
            {
                Target = _agentEntity,
                Trait = typeof(Inventory),
                Value = new NativeString64("test")
            }, childState);
        }

        [Test]
        public void NoInventoryGoal_NoNode()
        {
            _nodeGraphGroup.NodeStates.Remove(_goalNode);
            _nodeGraphGroup.NodeStates.Add(_goalNode, new State
            {
                Target = _containerEntity,
                Trait = typeof(GatherStation),
                Value = new NativeString64("test"),
            });
            
            _system.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.AreEqual(1, _nodeGraphGroup.Nodes.Length);
        }
    }
}