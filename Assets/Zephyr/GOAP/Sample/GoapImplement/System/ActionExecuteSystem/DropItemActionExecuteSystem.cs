using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    public class DropItemActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(FixedString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, NativeArray<GoalRefForNode> waitingNodeGoalRefs,
            BufferFromEntity<State> waitingStates, EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            return Entities.WithName("DropItemActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithReadOnly(waitingNodeEntities)
                .WithReadOnly(waitingNodes)
                .WithReadOnly(waitingNodeGoalRefs)
                .WithDisposeOnCompletion(waitingNodeEntities)
                .WithDisposeOnCompletion(waitingNodes)
                .WithDisposeOnCompletion(waitingNodeGoalRefs)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in DropItemAction action) =>
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
                        var targetAmount = 0;
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.EffectsBitmask & (ulong)1 << stateId) > 0)
                            {
                                var effect = states[stateId];
                                Assert.IsTrue(effect.Target!=null);
                        
                                targetEntity = effect.Target;
                                targetItemName = effect.ValueString;
                                targetAmount = effect.Amount;
                                break;
                            }
                        }
                        
                        var goalEntity = waitingNodeGoalRefs[nodeId].GoalEntity;
                        
                        //产生order
                        OrderWatchSystem.CreateOrderAndWatch<DropItemOrder>(ecb, entityInQueryIndex, agentEntity,
                            targetEntity, targetItemName, targetAmount, nodeEntity, Entity.Null);
                        
                        //进入执行中状态
                        Zephyr.GOAP.Utils.NextAgentState<ReadyToAct, Acting>(agentEntity, entityInQueryIndex,
                            ecb, nodeEntity);
                        
                        break;
                    }
                }).Schedule(inputDeps);
            ;
        }

        protected override FixedString32 GetNameOfAction()
        {
            return nameof(DropItemAction);
        }
    }
}