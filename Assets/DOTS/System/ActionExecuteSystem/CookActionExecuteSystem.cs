using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace DOTS.System.ActionExecuteSystem
{
    /// <summary>
    /// 在实际游戏中，应该是调用设施的制作方法并等待结果，示例从简，就直接进行物品处理了
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class CookActionExecuteSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        // [BurstCompile]
        [RequireComponentTag(typeof(CookAction), typeof(ReadyToActing))]
        public struct ActionExecuteJob : IJobForEachWithEntity_EBBBC<Node, State,
            ContainedItemRef, Agent>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, DynamicBuffer<ContainedItemRef> containedItems, ref Agent agent)
            {
                //执行进度要处于正确的id上
                var currentNode = nodes[agent.ExecutingNodeId];
                if (!currentNode.Name.Equals(new NativeString64(nameof(CookAction))))
                    return;

                //从precondition里找原料
                var inputItemNames = new NativeHashMap<NativeString64, int>(2, Allocator.Temp);
                for (var i = 0; i < states.Length; i++)
                {
                    if ((currentNode.PreconditionsBitmask & (ulong) 1 << i) <= 0) continue;
                    var precondition = states[i];
                    if (precondition.Trait != typeof(ItemContainerTrait)) continue;
                    var itemName = precondition.ValueString;
                    Assert.IsFalse(itemName.Equals(new NativeString64()));
                    if (!inputItemNames.ContainsKey(itemName))
                    {
                        inputItemNames.TryAdd(itemName, 1);
                    }
                    else
                    {
                        inputItemNames[precondition.ValueString] ++;
                    }
                }
                //从effect获取产物
                var outputItemName = new NativeString64();
                for (var i = 0; i < states.Length; i++)
                {
                    if ((currentNode.EffectsBitmask & (ulong) 1 << i) <= 0) continue;
                    var itemName = states[i].ValueString;
                    Assert.IsFalse(itemName.Equals(new NativeString64()));
                    outputItemName = itemName;
                    break;
                }
                
                //从自身找到原料物品引用，并移除
                //简便考虑，示例项目就不真的移除物品entity了
                for (var i = containedItems.Length - 1; i >= 0; i--)
                {
                    var containedItemRef = containedItems[i];
                    if (!inputItemNames.ContainsKey(containedItemRef.ItemName)) continue;
                    if (inputItemNames[containedItemRef.ItemName] == 0) continue;
                    containedItems.RemoveAt(i);
                }
                inputItemNames.Dispose();

                //自身获得产物
                //简便考虑，示例项目就不真的创建物品entity了
                containedItems.Add(new ContainedItemRef {ItemName = outputItemName});
                
                //通知执行完毕
                Utils.NextAgentState<ReadyToActing, ReadyToNavigating>(
                    entity, jobIndex, ref ECBuffer, agent, true);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new ActionExecuteJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var handle = job.Schedule(this, inputDeps);
            ECBSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}