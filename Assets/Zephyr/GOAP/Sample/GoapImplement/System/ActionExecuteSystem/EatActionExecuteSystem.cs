using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    public class EatActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(NativeString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.Concurrent ecb, JobHandle inputDeps)
        {
            ComponentType itemDestType = typeof(ItemDestinationTrait);
            var allBufferItems = GetBufferFromEntity<ContainedItemRef>();
            return Entities.WithName("EatActionExecuteJob")
                .WithoutBurst()    //由于示例里要用到string的物品名称，只能关闭burst
                .WithAll<ReadyToAct>()
                .WithNativeDisableParallelForRestriction(allBufferItems)
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex, ref Stamina stamina,
                    in Agent agent, in EatAction action) =>
                {
                    for (var nodeId = 0; nodeId < waitingNodeEntities.Length; nodeId++)
                    {
                        var nodeEntity = waitingNodeEntities[nodeId];
                        var node = waitingNodes[nodeId];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        var states = waitingStates[nodeEntity];
                        //从precondition里找食物.此时餐桌应该已经具有指定的食物
                        var targetItemName = new NativeString32();
                        var tableEntity = Entity.Null;
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.PreconditionsBitmask & (ulong) 1 << stateId) <= 0) continue;
                        
                            var precondition = states[stateId];
                            if (precondition.Trait != itemDestType) continue;
                        
                            targetItemName = precondition.ValueString;
                            tableEntity = precondition.Target;
                            break;
                        }
                        Assert.AreNotEqual(default, targetItemName);
                        
                        //从餐桌找到物品引用，并移除
                        var buffer = allBufferItems[tableEntity];
                        for (var itemId = 0; itemId < buffer.Length; itemId++)
                        {
                            var containedItemRef = buffer[itemId];
                            if (!containedItemRef.ItemName.Equals(targetItemName)) continue;
                            buffer.RemoveAt(itemId);
                            break;
                        }
                
                        //获得体力
                        //todo 正式游戏应当从食物数据中确认应该获得多少体力，并且由专用system负责吃的行为
                        stamina.Value += Utils.GetFoodStamina(targetItemName);

                        //通知执行完毕
                        Zephyr.GOAP.Utils.NextAgentState<ReadyToAct, ActDone>(agentEntity, entityInQueryIndex,
                            ecb, nodeEntity);

                        //node指示执行完毕 
                        Zephyr.GOAP.Utils.NextActionNodeState<ActionNodeActing, ActionNodeDone>(nodeEntity,
                            entityInQueryIndex,
                            ecb, agentEntity);
                        break;
                    }
                }).Schedule(inputDeps);
            ;
        }

        protected override NativeString32 GetNameOfAction()
        {
            return nameof(EatAction);
        }
    }
}