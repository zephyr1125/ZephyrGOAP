using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine.Assertions;

namespace DOTS.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class NavigatingStartSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(ReadyToNavigating))]
        private struct NavigatingStartJob : IJobForEachWithEntity_EBBC<Node, State, Agent>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;

            public ComponentDataFromEntity<Translation> Translations;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent)
            {
                var currentNode = nodes[agent.ExecutingNodeId];
                //todo 目标不一定来自effect,在哪儿写明导航目标呢
                //从effect获取目标
                var targetEntity = Entity.Null;
                for (var i = 0; i < states.Length; i++)
                {
                    if ((currentNode.EffectsBitmask & (ulong)1 << i) > 0)
                    {
                        var effect = states[i];
                        Assert.IsTrue(effect.Target!=null);
                        
                        targetEntity = effect.Target;
                        break;
                    }
                }

                if (targetEntity == entity)
                {
                    //目标为agent自身，无需移动，直接跳到ReadyToActing
                    Utils.NextAgentState<ReadyToNavigating, ReadyToActing>(
                        entity, jobIndex, ref ECBuffer, agent, false);
                    return;
                }
                
                Assert.IsTrue(Translations.HasComponent(targetEntity),
                    "Target should has translation");
                
                //todo 路径规划
                
                //设置target,通知开始移动
                ECBuffer.SetComponent(jobIndex, entity,
                    new TargetPosition{Value = Translations[targetEntity].Value});
                
                //等待移动结束
                Utils.NextAgentState<ReadyToNavigating, Navigating>(entity, jobIndex,
                    ref ECBuffer, agent, false);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}