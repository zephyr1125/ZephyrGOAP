using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.ActionExecuteSystem
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class PickRawActionExecuteSystem : JobComponentSystem
    {
        private EntityQuery _waitingActionNodeQuery;

        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _waitingActionNodeQuery = GetEntityQuery(new EntityQueryDesc{
                All =  new []{ComponentType.ReadOnly<Node>()},
                None = new []{ComponentType.ReadOnly<NodeDependency>()}});
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //找出所有可执行的PickRawAction
            var waitingNodeEntities = _waitingActionNodeQuery.ToEntityArray(Allocator.TempJob);
            var waitingNodes =
                _waitingActionNodeQuery.ToComponentDataArray<Node>(Allocator.TempJob);
            var waitingStates = GetBufferFromEntity<State>();

            var ecb = EcbSystem.CreateCommandBuffer().ToConcurrent();

            var handle = Entities.WithName("PickRawActionExecuteJob")
                .WithAll<ReadyToAct>()
                .WithDeallocateOnJobCompletion(waitingNodeEntities)
                .WithDeallocateOnJobCompletion(waitingNodes)
                .WithReadOnly(waitingStates)
                .ForEach((Entity agentEntity, int entityInQueryIndex,
                    DynamicBuffer<ContainedItemRef> containedItemRefs,
                    in Agent agent, in PickRawAction pickRawAction) =>
                {
                    for (var i = 0; i < waitingNodeEntities.Length; i++)
                    {
                        var node = waitingNodes[i];
                        if (!node.AgentExecutorEntity.Equals(agentEntity)) continue;
                        if (!node.Name.Equals(nameof(PickRawAction))) continue;

                        var states = waitingStates[waitingNodeEntities[i]];
                        //从precondition里找物品名.
                        var targetItemName = new NativeString64();
                        for (var stateId = 0; stateId < states.Length; stateId++)
                        {
                            if ((node.PreconditionsBitmask & (ulong) 1 << stateId) <= 0) continue;
                            
                            var precondition = states[i];
                            Assert.IsTrue(precondition.Target!=null);
                        
                            targetItemName = precondition.ValueString;
                            break;
                        }
                        //todo 目前原料源不使用物品容器，直接提供无限的原料物品

                        //自己获得物品
                        containedItemRefs.Add(new ContainedItemRef{ItemName = targetItemName});
                
                        //通知执行完毕
                        Utils.NextAgentState<ReadyToAct, ReadyToNavigate>(agentEntity, entityInQueryIndex, ref ecb);
                        
                        //todo node指示执行完毕 
                        return;
                    }
                }).Schedule(inputDeps);
             
                EcbSystem.AddJobHandleForProducer(handle);

                return handle;
        }
    }
}