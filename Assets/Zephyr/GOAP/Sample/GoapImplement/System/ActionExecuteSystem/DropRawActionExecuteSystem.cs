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
        protected override JobHandle ExecuteActionJob(FixedString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            var allContainedItemRefs = GetBufferFromEntity<ContainedItemRef>(true);
            var allCounts = GetComponentDataFromEntity<Count>(true);
            return Entities.WithName("DropRawActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithDisposeOnCompletion(waitingNodeEntities)
                .WithDisposeOnCompletion(waitingNodes)
                .WithReadOnly(allContainedItemRefs)
                .WithReadOnly(allCounts)
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
                        var targetItemName = new FixedString32();
                        byte targetAmount = 0;
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.EffectsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            var effect = states[stateId];
                            Assert.IsTrue(effect.Target!=null);
                        
                            targetEntity = effect.Target;
                            targetItemName = effect.ValueString;
                            targetAmount = effect.Amount;
                            break;
                        }
                        
                        //自身减少物品
                        var agentItems = allContainedItemRefs[agentEntity];
                        Utils.ModifyItemInContainer(entityInQueryIndex, ecb, agentEntity,
                            agentItems, allCounts, targetItemName, -targetAmount);

                        //目标增加物品
                        var targetItems = allContainedItemRefs[targetEntity];
                        Utils.ModifyItemInContainer(entityInQueryIndex, ecb, targetEntity,
                            targetItems, allCounts, targetItemName, targetAmount);

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
        }

        protected override FixedString32 GetNameOfAction()
        {
            return nameof(DropRawAction);
        }
    }
}