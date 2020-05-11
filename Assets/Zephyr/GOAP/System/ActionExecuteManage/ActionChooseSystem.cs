using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.ActionExecuteManage
{
    /// <summary>
    /// agent从action nodes里挑选最旧的一个适合自己现在做的
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class ActionChooseSystem : JobComponentSystem
    {
        private EntityQuery _waitingActionNodeQuery;

        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _waitingActionNodeQuery = GetEntityQuery(new EntityQueryDesc{
                All =  new []{ComponentType.ReadOnly<Node>()},
                None = new []{ComponentType.ReadOnly<NodeDependency>(), ComponentType.ReadOnly<ActionNodeActing>(), }});
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //找出所有可执行的Node
            var waitingNodeEntities = _waitingActionNodeQuery.ToEntityArray(Allocator.TempJob);
            var waitingNodes =
                _waitingActionNodeQuery.ToComponentDataArray<Node>(Allocator.TempJob);

            var ecb = EcbSystem.CreateCommandBuffer().ToConcurrent();

            var handle = Entities.WithName("ActionChooseJob")
                .WithAll<Idle, Agent>()
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .ForEach((Entity agentEntity, int entityInQueryIndex) =>
                {
                    //筛出给我的action
                    var availableNodeEntities = new NativeMinHeap<Entity>(waitingNodeEntities.Length, Allocator.Temp);
                    for (var i = 0; i < waitingNodeEntities.Length; i++)
                    {
                        var node = waitingNodes[i];
                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        availableNodeEntities.Push(new MinHeapNode<Entity>(waitingNodeEntities[i], node.EstimateStartTime));
                    }
                    //寻找最早的一个去执行
                    var oldestNodeEntity = availableNodeEntities[availableNodeEntities.Pop()].Content;
                    
                    //双向链接保存记录
                    Utils.NextAgentState<Idle, ReadyToNavigate>(agentEntity, entityInQueryIndex, ref ecb, oldestNodeEntity);
                    ecb.AddComponent(entityInQueryIndex, oldestNodeEntity, new ActionNodeActing{AgentEntity = agentEntity});
                    
                    availableNodeEntities.Dispose();
                    
                }).Schedule(inputDeps);
             
            EcbSystem.AddJobHandleForProducer(handle);

            return handle;
        }
    }
}