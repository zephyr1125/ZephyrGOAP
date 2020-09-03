using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    public class PickItemActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(FixedString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            return Entities.WithName("PickItemActionExecuteJob")
                .WithAll<Agent>()
                .WithAll<PickItemAction>()
                .WithAll<ReadyToAct>()
                .WithDisposeOnCompletion(waitingNodeEntities)
                .WithDisposeOnCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex) =>
                {
                    for (var nodeId = 0; nodeId < waitingNodeEntities.Length; nodeId++)
                    {
                        var nodeEntity = waitingNodeEntities[nodeId];
                        var node = waitingNodes[nodeId];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;
                        
                        var states = waitingStates[nodeEntity];
                        var prevOrderEntity = Entity.Null;
                        //从precondition里找信息，可能因为多重来源被拆分为多个
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.PreconditionsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            var precondition = states[stateId];
                            Assert.IsTrue(precondition.Target != Entity.Null);
                            
                            var itemEntity = precondition.Target;
                            var itemName = precondition.ValueString;
                            var amount = precondition.Amount;
                            
                            //产生order
                            prevOrderEntity = OrderWatchSystem.CreateOrderAndWatch<PickItemOrder>(ecb, entityInQueryIndex, agentEntity,
                                itemEntity, itemName, amount, nodeEntity, prevOrderEntity);
                        }
                        
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
            return nameof(PickItemAction);
        }
    }
}