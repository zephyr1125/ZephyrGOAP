using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.ActionExecuteManage
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NavigatingStartSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var nodes = GetComponentDataFromEntity<Node>();
            var translations = GetComponentDataFromEntity<Translation>();
            var ecb = EcbSystem.CreateCommandBuffer().ToConcurrent();
            var handle = Entities
                .WithReadOnly(nodes)
                .WithReadOnly(translations)
                .WithAll<Agent>()
                .ForEach((Entity entity, int entityInQueryIndex, in ReadyToNavigate readyToNavigate) =>
                {
                    var node = nodes[readyToNavigate.NodeEntity];
                    //从node获取目标
                    var targetEntity = node.NavigatingSubject;

                    if (targetEntity == entity || targetEntity==Entity.Null)
                    {
                        //目标为空或agent自身，无需移动，直接跳到ReadyToActing
                        Utils.NextAgentState<ReadyToNavigate, ReadyToAct>(
                            entity, entityInQueryIndex, ref ecb, readyToNavigate.NodeEntity);
                        return;
                    }
                
                    Assert.IsTrue(translations.HasComponent(targetEntity),
                        "Target should has translation");
                
                    //todo 路径规划
                
                    //设置target,通知开始移动
                    ecb.AddComponent(entityInQueryIndex, entity,
                        new TargetPosition{Value = translations[targetEntity].Value});
                
                    //切换agent状态,等待移动结束
                    Utils.NextAgentState<ReadyToNavigate, Navigating>(entity, entityInQueryIndex,
                        ref ecb, readyToNavigate.NodeEntity);
                    
                }).Schedule(inputDeps);
            EcbSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}