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
    public class DropRawActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(NativeString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.Concurrent ecb, JobHandle inputDeps)
        {
            var allBufferItems = GetBufferFromEntity<ContainedItemRef>();
            return Entities.WithName("DropRawActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithNativeDisableParallelForRestriction(allBufferItems)
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in DropRawAction action) =>
                {
                    for (var nodeId = 0; nodeId < waitingNodeEntities.Length; nodeId++)
                    {
                        var nodeEntity = waitingNodeEntities[nodeId];
                        var node = waitingNodes[nodeId];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        var states = waitingStates[nodeEntity];
                        //从effect里找目标.
                        var targetEntity = Entity.Null;
                        var targetItemName = new NativeString32();
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.EffectsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            var effect = states[stateId];
                            Assert.IsTrue(effect.Target!=null);
                        
                            targetEntity = effect.Target;
                            targetItemName = effect.ValueString;
                            break;
                        }
                        //从自身找到物品引用，并移除
                        var agentItems = allBufferItems[agentEntity];
                        var itemRef = new ContainedItemRef();
                        for (var itemId = 0; itemId < agentItems.Length; itemId++)
                        {
                            var containedItemRef = agentItems[itemId];
                            if (!containedItemRef.ItemName.Equals(targetItemName)) continue;
                            itemRef = containedItemRef;
                            agentItems.RemoveAt(itemId);
                            break;
                        }

                        //目标获得物品
                        var targetItems = allBufferItems[targetEntity];
                        targetItems.Add(itemRef);

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
            return nameof(DropRawAction);
        }
    }
}