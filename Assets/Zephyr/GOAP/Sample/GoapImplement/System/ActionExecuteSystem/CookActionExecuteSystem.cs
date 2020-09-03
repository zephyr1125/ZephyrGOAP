using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    
    
    /// <summary>
    /// 在实际游戏中，应该是调用设施的制作方法并等待结果，示例从简，就直接进行物品处理了
    /// </summary>
    public class CookActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(FixedString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            var actionType = TypeManager.GetTypeIndex<ItemDestinationTrait>();
            return Entities.WithName("CookActionExecuteStartJob")
                .WithAll<ReadyToAct>()
                .WithDisposeOnCompletion(waitingNodeEntities)
                .WithDisposeOnCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in CookAction action) =>
                {
                    for (var nodeId = 0; nodeId < waitingNodeEntities.Length; nodeId++)
                    {
                        var nodeEntity = waitingNodeEntities[nodeId];
                        var node = waitingNodes[nodeId];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        var states = waitingStates[nodeEntity];
                        //从precondition里找CookerEntity
                        var cookerEntity = Entity.Null;
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.PreconditionsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            var precondition = states[stateId];
                            if (precondition.Trait != actionType) continue;
                            cookerEntity = precondition.Target;
                        }
                        //从effect获取产物
                        var outputItemName = new FixedString32();
                        var outputAmount = 0;
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.EffectsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            var effect = states[stateId];
                            var itemName = effect.ValueString;
                            Assert.IsFalse(itemName.Equals(new FixedString32()));
                            outputItemName = itemName;
                            outputAmount = effect.Amount;
                            break;
                        }
                        
                        //产生order
                        OrderWatchSystem.CreateOrderAndWatch<CookOrder>(ecb, entityInQueryIndex, agentEntity,
                            cookerEntity, outputItemName, outputAmount, nodeEntity, Entity.Null);
                        
                        //进入执行中状态
                        Zephyr.GOAP.Utils.NextAgentState<ReadyToAct, Acting>(agentEntity, entityInQueryIndex,
                            ecb, nodeEntity);

                        break;
                    }
                }).Schedule(inputDeps);
        }

        protected override FixedString32 GetNameOfAction()
        {
            return nameof(CookAction);
        }
    }
}