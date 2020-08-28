using NUnit.Framework;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Tests.Mock;

namespace Zephyr.GOAP.Tests.ActionExecute
{
    /// <summary>
    /// 对各个ActionExecuteSystem共通的一些测试
    /// </summary>
    public class TestActionExecuteSystemBase : TestActionExecuteBase
    {
        private MockActionExecuteSystem _system;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<MockActionExecuteSystem>();
            
            EntityManager.AddComponentData(_agentEntity, new MockProduceAction());
            
            EntityManager.AddComponentData(_actionNodeEntity, new Node
            {
                AgentExecutorEntity = _agentEntity,
                Name = nameof(MockProduceAction),
            });
        }

        /// <summary>
        /// 各个具体ActionExecuteSystem就不用测试ProgressGoOn了
        /// </summary>
        [Test]
        public void ProgressGoOn()
        {
            _system.Update();
            _system.EcbSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.False(EntityManager.HasComponent<ReadyToAct>(_agentEntity));
            Assert.True(EntityManager.HasComponent<ActDone>(_agentEntity));
            
            Assert.False(EntityManager.HasComponent<ActionNodeActing>(_actionNodeEntity));
            Assert.True(EntityManager.HasComponent<ActionNodeDone>(_actionNodeEntity));
        }
    }
}