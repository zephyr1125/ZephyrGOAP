using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    /// <summary>
    /// ReadyToActing时请求Wander
    /// Acting时等待Wander结束
    /// </summary>
    public class WanderActionExecuteSystem : ActionExecuteSystemBase
    {
        /// <summary>
        /// 示例里固定wander时间5s
        /// </summary>
        public const float WanderTime = 5;

        protected override JobHandle ExecuteActionJob(FixedString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            return Entities.WithName("WanderActionExecuteJob")
                .WithAll<ReadyToAct>()
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in WanderAction action) =>
                {
                    for (var nodeId = 0; nodeId < waitingNodeEntities.Length; nodeId++)
                    {
                        var nodeEntity = waitingNodeEntities[nodeId];
                        var node = waitingNodes[nodeId];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        ecb.AddComponent(entityInQueryIndex, agentEntity, new Wander{Time = WanderTime});

                        //进入执行中状态
                        Zephyr.GOAP.Utils.NextAgentState<ReadyToAct, Acting>(agentEntity, entityInQueryIndex,
                            ecb, nodeEntity);
                        break;
                    }
                }).Schedule(inputDeps);
            ;
        }
        
        /// <summary>
        /// 使用第二个Job来监控Wander完毕
        /// </summary>
        /// <param name="nameOfAction"></param>
        /// <param name="waitingNodeEntities"></param>
        /// <param name="waitingNodes"></param>
        /// <param name="waitingStates"></param>
        /// <param name="ecb"></param>
        /// <param name="inputDeps"></param>
        /// <returns></returns>
        protected override JobHandle ExecuteActionJob2(FixedString32 nameOfAction, NativeArray<Entity> waitingNodeEntities,
            NativeArray<Node> waitingNodes, BufferFromEntity<State> waitingStates, EntityCommandBuffer.ParallelWriter ecb, JobHandle inputDeps)
        {
            return Entities.WithName("WanderActionDoneJob")
                .WithAll<Acting>()
                .WithNone<Wander>()
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    in Agent agent, in WanderAction action) =>
                {
                    for (var i = 0; i < waitingNodeEntities.Length; i++)
                    {
                        var nodeEntity = waitingNodeEntities[i];
                        var node = waitingNodes[i];

                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameOfAction)) continue;

                        //agent指示执行完毕
                        Zephyr.GOAP.Utils.NextAgentState<Acting, ActDone>(agentEntity, entityInQueryIndex,
                            ecb, nodeEntity);

                        //node指示执行完毕 
                        Zephyr.GOAP.Utils.NextActionNodeState<ActionNodeActing, ActionNodeDone>(nodeEntity,
                            entityInQueryIndex, ecb, agentEntity);
                        break;
                    }
                }).Schedule(inputDeps);
            ;
        }

        protected override FixedString32 GetNameOfAction()
        {
            return nameof(WanderAction);
        }
    }
}