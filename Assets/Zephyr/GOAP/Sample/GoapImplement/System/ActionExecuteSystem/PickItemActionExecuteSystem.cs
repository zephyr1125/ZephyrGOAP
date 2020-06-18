using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    public class PickItemActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(NativeString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.Concurrent ecb, JobHandle inputDeps)
        {
            var allBufferItems = GetBufferFromEntity<ContainedItemRef>();
            return Entities.WithName("PickItemActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithNativeDisableParallelForRestriction(allBufferItems)
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in PickItemAction action) =>
                {
                    for (var i = 0; i < waitingNodeEntities.Length; i++)
                    {
                        var nodeEntity = waitingNodeEntities[i];
                        var node = waitingNodes[i];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        var states = waitingStates[nodeEntity];
                        //从precondition里找目标.
                        var targetEntity = Entity.Null;
                        var targetItemName = new NativeString32();
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.PreconditionsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            var precondition = states[stateId];
                            Assert.IsTrue(precondition.Target!=null);
                        
                            targetEntity = precondition.Target;
                            targetItemName = precondition.ValueString;
                            break;
                        }
                        //从目标身上找到物品引用，并移除
                        var itemRef = new ContainedItemRef();
                        var id = 0;
                        var bufferContainedItems = allBufferItems[targetEntity];
                        for (var itemId = 0; itemId < bufferContainedItems.Length; itemId++)
                        {
                            var containedItemRef = bufferContainedItems[itemId];
                            if (!containedItemRef.ItemName.Equals(targetItemName)) continue;
                    
                            itemRef = containedItemRef;
                            id = itemId;
                            break;
                        }
                        bufferContainedItems.RemoveAt(id);

                        //自己获得物品
                        var buffer = allBufferItems[agentEntity];
                        buffer.Add(itemRef);

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
            ;
        }

        protected override NativeString32 GetNameOfAction()
        {
            return nameof(PickItemAction);
        }
    }
}