using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.ActionExecuteManage
{
    /// <summary>
    /// 执行结束,Action删除，关联Dependency清理，Agent空闲
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class ActionExecuteDoneSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem EcbSystem;

        private EntityQuery _doneNodesQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            var ecbConcurrent = ecb.AsParallelWriter();
            var doneNodesCount = _doneNodesQuery.CalculateEntityCount();
            var agentEntities = new NativeArray<Entity>(doneNodesCount, Allocator.TempJob);
            
            //移除nodes
            var removeHandle = Entities.WithName("RemoveDoneNodeJob")
                .WithAll<ActionNodeDone>()
                .WithStoreEntityQueryInField(ref _doneNodesQuery)
                .ForEach((Entity nodeEntity, int entityInQueryIndex, in Node node) =>
                {
                    agentEntities[entityInQueryIndex] = node.AgentExecutorEntity;
                    ecbConcurrent.DestroyEntity(entityInQueryIndex, nodeEntity);
                }).Schedule(inputDeps);
            EcbSystem.AddJobHandleForProducer(removeHandle);
            
            //清理所有相关dependency
            var doneNodeEntities = _doneNodesQuery.ToEntityArray(Allocator.TempJob);
            var cleanDependencyHandle = Entities.WithName("CleanDependencyJob")
                .WithReadOnly(doneNodeEntities)
                .ForEach((Entity nodeEntity, int entityInQueryIndex,
                DynamicBuffer<NodeDependency> bufferDependency) =>
            {
                for (var i = 0; i < bufferDependency.Length; i++)
                {
                    if (!doneNodeEntities.Contains(bufferDependency[i].Entity)) continue;
                    bufferDependency.RemoveAt(i);
                    break;
                }
                
                //已经没有dependency的话，删除buffer
                if (bufferDependency.Length > 0) return;
                ecbConcurrent.RemoveComponent<NodeDependency>(entityInQueryIndex, nodeEntity);
            }).Schedule(removeHandle);
            
            //清理对应的delta states
            var cleanDeltaStatesHandle = Entities.WithName("CleanDeltaStatesJob")
                .WithReadOnly(doneNodeEntities)
                .WithDisposeOnCompletion(doneNodeEntities)
                .ForEach((Entity deltaStatesEntity, int entityInQueryIndex, in DeltaStates deltaStates) =>
                {
                    if (!doneNodeEntities.Contains(deltaStates.ActionNodeEntity)) return;
                    ecbConcurrent.DestroyEntity(entityInQueryIndex, deltaStatesEntity);
                }).Schedule(cleanDependencyHandle);
            
            EcbSystem.AddJobHandleForProducer(cleanDeltaStatesHandle);
            
            //复位agent状态
            var resetAgentHandle = Job.WithName("ResetAgentStateJob")
                .WithDisposeOnCompletion(agentEntities)
                .WithCode(() =>
                {
                    for (var i = 0; i < agentEntities.Length; i++)
                    {
                        Utils.NextAgentState<ActDone, Idle>(agentEntities[i], ecb, Entity.Null);
                    }
                }).Schedule(cleanDeltaStatesHandle);
            EcbSystem.AddJobHandleForProducer(resetAgentHandle);
            
            return resetAgentHandle;
        }
    }
}