using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    public class PickRawActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override FixedString32 GetNameOfAction()
        {
            return nameof(PickRawAction);
        }

        protected override JobHandle ExecuteActionJob(FixedString32 nameOfAction,
            NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates,
            EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            return Entities.WithName("PickRawActionExecuteJob")
                .WithAll<Agent>()
                .WithAll<PickRawAction>()
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
                            
                            var rawEntity = precondition.Target;
                            var rawItemName = precondition.ValueString;
                            var rawAmount = precondition.Amount;
                            
                            //产生order
                            prevOrderEntity = OrderWatchSystem.CreateOrderAndWatch<PickRawOrder>(ecb, entityInQueryIndex, agentEntity,
                                rawEntity, rawItemName, rawAmount, nodeEntity, prevOrderEntity);
                        }
                        
                        
                        //进入执行中状态
                        Zephyr.GOAP.Utils.NextAgentState<ReadyToAct, Acting>(agentEntity, entityInQueryIndex,
                            ecb, nodeEntity);
                        
                        break;
                    }
                }).Schedule(inputDeps);
        }
    }
}