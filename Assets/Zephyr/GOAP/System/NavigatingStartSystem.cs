using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NavigatingStartSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        // [BurstCompile]
        [RequireComponentTag(typeof(ReadyToNavigate))]
        private struct NavigatingStartJob : IJobForEachWithEntity_EBC<Node, Agent>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;

            [ReadOnly]
            public ComponentDataFromEntity<Translation> Translations;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes, ref Agent agent)
            {
                //id超过长度表示运行结束了
                if (agent.ExecutingNodeId >= nodes.Length) return;
                
                var currentNode = nodes[agent.ExecutingNodeId];
                //从node获取目标
                var targetEntity = currentNode.NavigatingSubject;

                if (targetEntity == entity || targetEntity==Entity.Null)
                {
                    //目标为空或agent自身，无需移动，直接跳到ReadyToActing
                    Utils.NextAgentState<ReadyToNavigate, ReadyToAct>(
                        entity, jobIndex, ref ECBuffer, agent, false);
                    return;
                }
                
                Assert.IsTrue(Translations.HasComponent(targetEntity),
                    "Target should has translation");
                
                //todo 路径规划
                
                //设置target,通知开始移动
                ECBuffer.AddComponent(jobIndex, entity,
                    new TargetPosition{Value = Translations[targetEntity].Value});
                
                //切换agent状态,等待移动结束
                Utils.NextAgentState<ReadyToNavigate, Navigating>(entity, jobIndex,
                    ref ECBuffer, agent, false);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new NavigatingStartJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent(),
                Translations = GetComponentDataFromEntity<Translation>()
            };
            var handle = job.Schedule(this, inputDeps);
            ECBSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}