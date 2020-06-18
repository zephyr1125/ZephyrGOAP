using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    /// <summary>
    /// 在实际游戏中，应该是调用设施的制作方法并等待结果，示例从简，就直接进行物品处理了
    /// </summary>
    public class CookActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(NativeString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.Concurrent ecb, JobHandle inputDeps)
        {
            ComponentType actionType = typeof(ItemDestinationTrait);
            var allItems = GetBufferFromEntity<ContainedItemRef>();
            return Entities.WithName("CookActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithNativeDisableParallelForRestriction(allItems)
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in CookAction action) =>
                {
                    for (var i = 0; i < waitingNodeEntities.Length; i++)
                    {
                        var nodeEntity = waitingNodeEntities[i];
                        var node = waitingNodes[i];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        var states = waitingStates[nodeEntity];
                        //从precondition里找CookerEntity以及原料
                        var cookerEntity = Entity.Null;
                        var inputItemNames = new NativeHashMap<NativeString32, int>(2, Allocator.Temp);
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.PreconditionsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            var precondition = states[stateId];
                            if (precondition.Trait != actionType) continue;
                            cookerEntity = precondition.Target;
                            var itemName = precondition.ValueString;
                            Assert.IsFalse(itemName.Equals(new NativeString32()));
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
                        var outputItemName = new NativeString32();
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.EffectsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            var itemName = states[stateId].ValueString;
                            Assert.IsFalse(itemName.Equals(default));
                            outputItemName = itemName;
                            break;
                        }
                        
                        //从cooker容器找到原料物品引用，并移除
                        var itemsInCooker = allItems[cookerEntity];
                        //简便考虑，示例项目就不真的移除物品entity了
                        for (var itemId = itemsInCooker.Length - 1; itemId >= 0; itemId--)
                        {
                            var containedItemRef = itemsInCooker[itemId];
                            if (!inputItemNames.ContainsKey(containedItemRef.ItemName)) continue;
                            if (inputItemNames[containedItemRef.ItemName] == 0) continue;
                            itemsInCooker.RemoveAt(itemId);
                        }
                        inputItemNames.Dispose();

                        //cooker容器获得产物
                        //简便考虑，示例项目就不真的创建物品entity了
                        itemsInCooker.Add(new ContainedItemRef {ItemName = outputItemName});
                        
                        //通知执行完毕
                        Zephyr.GOAP.Utils.NextAgentState<ReadyToAct, ActDone>(agentEntity, entityInQueryIndex,
                            ref ecb, nodeEntity);

                        //node指示执行完毕 
                        Zephyr.GOAP.Utils.NextActionNodeState<ActionNodeActing, ActionNodeDone>(nodeEntity,
                            entityInQueryIndex,
                            ref ecb, agentEntity);
                        break;
                    }
                }).Schedule(inputDeps);
        }

        protected override NativeString32 GetNameOfAction()
        {
            return nameof(CookAction);
        }
    }
}