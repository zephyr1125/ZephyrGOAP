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
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    public class EatActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(FixedString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            var itemDestType = TypeManager.GetTypeIndex<ItemDestinationTrait>();
            return Entities.WithName("EatActionExecuteJob")
                //.WithoutBurst()    //由于示例里要用到string的物品名称，只能关闭burst
                .WithAll<ReadyToAct>()
                .WithDisposeOnCompletion(waitingNodeEntities)
                .WithDisposeOnCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
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
                        var targetItemName = new FixedString32();
                        var tableEntity = Entity.Null;
                        var amount = 0;
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.PreconditionsBitmask & (ulong) 1 << stateId) <= 0) continue;
                        
                            var precondition = states[stateId];
                            if (precondition.Trait != itemDestType) continue;
                        
                            targetItemName = precondition.ValueString;
                            tableEntity = precondition.Target;
                            amount = precondition.Amount;
                            break;
                        }
                        Assert.AreNotEqual(default, targetItemName);
                        
                        //产生order
                        OrderWatchSystem.CreateOrderAndWatch<EatOrder>(ecb, entityInQueryIndex, agentEntity,
                            tableEntity, targetItemName, amount, nodeEntity, Entity.Null);
                        
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
            return nameof(EatAction);
        }
    }
}