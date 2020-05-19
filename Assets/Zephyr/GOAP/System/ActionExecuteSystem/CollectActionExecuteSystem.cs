using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.ActionExecuteSystem
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class CollectActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override JobHandle ExecuteActionJob(NativeString64 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.Concurrent ecb, JobHandle inputDeps)
        {
            return Entities.WithName("PickRawActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in CollectAction action) =>
                {
                    for (var i = 0; i < waitingNodeEntities.Length; i++)
                    {
                        var nodeEntity = waitingNodeEntities[i];
                        var node = waitingNodes[i];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        //CollectAction现在没有什么具体要做的事情，因为DropRawAction已经把物品放进去了
                        //而后续也有PickItemAction处理

                        //通知执行完毕
                        Utils.NextAgentState<ReadyToAct, ActDone>(agentEntity, entityInQueryIndex,
                            ref ecb, nodeEntity);

                        //node指示执行完毕 
                        Utils.NextActionNodeState<ActionNodeActing, ActionNodeDone>(nodeEntity,
                            entityInQueryIndex,
                            ref ecb, agentEntity);
                        break;
                    }
                }).Schedule(inputDeps);
        }

        protected override NativeString64 GetNameOfAction()
        {
            return nameof(CollectAction);
        }
    }
}