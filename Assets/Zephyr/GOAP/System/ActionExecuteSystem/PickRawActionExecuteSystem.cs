using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.ActionExecuteSystem
{
    public class PickRawActionExecuteSystem : ActionExecuteSystemBase
    {
        protected override NativeString64 GetNameOfAction()
        {
            return nameof(PickRawAction);
        }

        protected override JobHandle ExecuteActionJob(NativeString64 nameOfAction,
            NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates,
            EntityCommandBuffer.Concurrent ecb, JobHandle inputDeps)
        {
            return Entities.WithName("PickRawActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    DynamicBuffer<ContainedItemRef> containedItemRefs,
                    in Agent agent, in PickRawAction action) =>
                {
                    for (var i = 0; i < waitingNodeEntities.Length; i++)
                    {
                        var nodeEntity = waitingNodeEntities[i];
                        var node = waitingNodes[i];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        var states = waitingStates[nodeEntity];
                        //从precondition里找物品名.
                        var targetItemName = new NativeString64();
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.PreconditionsBitmask & (ulong) 1 << stateId) <= 0) continue;

                            var precondition = states[i];
                            Assert.IsTrue(precondition.Target != Entity.Null);

                            targetItemName = precondition.ValueString;
                            break;
                        }
                        //todo 目前原料源不使用物品容器，直接提供无限的原料物品

                        //自己获得物品
                        containedItemRefs.Add(new ContainedItemRef {ItemName = targetItemName});

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
    }
}