using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine.Assertions;

namespace DOTS.System.ActionExecuteSystem
{
    /// <summary>
    /// ReadyToActing时请求Wander
    /// Acting时等待Wander结束
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class WanderActionExecuteSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;

        /// <summary>
        /// 示例里固定wander时间5s
        /// </summary>
        public static float WanderTime = 5;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        /// <summary>
        /// 启动时给agent赋予Wander组件
        /// </summary>
        // [BurstCompile]
        [RequireComponentTag(typeof(WanderAction), typeof(ReadyToActing))]
        private struct ActionExecuteJob : IJobForEachWithEntity_EBBC<Node, State, Agent>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent)
            {
                //执行进度要处于正确的id上
                var currentNode = nodes[agent.ExecutingNodeId];
                if (!currentNode.Name.Equals(new NativeString64(nameof(WanderAction))))
                    return;
                
                ECBuffer.AddComponent(jobIndex, entity, new Wander{Time = WanderTime});

                //切换状态，在监视的job里通知执行完毕
                Utils.NextAgentState<ReadyToActing, Acting>(entity, jobIndex,
                    ref ECBuffer, agent, false);
            }
        }
        
        /// <summary>
        /// 监视执行完毕后，向上通知
        /// </summary>
        // [BurstCompile]
        [RequireComponentTag(typeof(WanderAction), typeof(Acting))]
        [ExcludeComponent(typeof(ReadyToActing), typeof(Wander))]
        private struct ActionDoneJob : IJobForEachWithEntity_EBBC<Node, State, Agent>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent)
            {
                //执行进度要处于正确的id上
                var currentNode = nodes[agent.ExecutingNodeId];
                if (!currentNode.Name.Equals(new NativeString64(nameof(WanderAction))))
                    return;
                
                //通知执行完毕
                Utils.NextAgentState<Acting, ReadyToNavigating>(
                    entity, jobIndex, ref ECBuffer, agent, true);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var executeJob = new ActionExecuteJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var doneJob = new ActionDoneJob()
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            
            var executeHandle = executeJob.Schedule(this, inputDeps);
            var doneHandle = doneJob.Schedule(this, executeHandle);
            
            ECBSystem.AddJobHandleForProducer(executeHandle);
            ECBSystem.AddJobHandleForProducer(doneHandle);
            
            return doneHandle;
        }
    }
}