using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    public class CollectActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(NativeString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.Concurrent ecb, JobHandle inputDeps)
        {
            return Entities.WithName("CollectActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in CollectAction action) =>
                {
                    for (var nodeId = 0; nodeId < waitingNodeEntities.Length; nodeId++)
                    {
                        var nodeEntity = waitingNodeEntities[nodeId];
                        var node = waitingNodes[nodeId];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        //CollectAction现在没有什么具体要做的事情，因为DropRawAction已经把物品放进去了
                        //而后续也有PickItemAction处理

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
            return nameof(CollectAction);
        }
    }
}