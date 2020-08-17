using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component.Order;

namespace Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem
{
    /// <summary>
    /// 有一些ActionExecute本身不执行具体行为，而是发出Order交给具体执行的系统去做
    /// 之后就由这个系统来监控order的结束并通知action执行完成
    /// Game的OrderCleanSystem会在order执行完毕后destroy他
    /// 而如果是来自action的order会被附加state component，得以监视到destroy
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class OrderWatchSystem : SystemBase
    {
        /// <summary>
        /// 监控Order
        /// </summary>
        public struct OrderWatch : ISystemStateComponentData
        {
            public Entity AgentEntity, NodeEntity;
        }

        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var handle = Entities.WithName("OrderWatchJob")
                .WithNone<Order>()
                .ForEach((Entity orderEntity, int entityInQueryIndex, in OrderWatch orderWatch) =>
                {
                    var agentEntity = orderWatch.AgentEntity;
                    var nodeEntity = orderWatch.NodeEntity;

                    //通知执行完毕
                    Zephyr.GOAP.Utils.NextAgentState<ReadyToAct, ActDone>(agentEntity, entityInQueryIndex,
                        ecb, nodeEntity);
                    
                    //node指示执行完毕 
                    Zephyr.GOAP.Utils.NextActionNodeState<ActionNodeActing, ActionNodeDone>(nodeEntity,
                        entityInQueryIndex, ecb, agentEntity);
                    
                    ecb.RemoveComponent<OrderWatch>(entityInQueryIndex, orderEntity);
                }).Schedule(Dependency);
            EcbSystem.AddJobHandleForProducer(handle);
        }
        
        public static void CreateOrderAndWatch(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex,
            Entity agentEntity, Entity facilityEntity, FixedString32 outputItemName, byte outputAmount,
            Entity nodeEntity)
        {
            var orderEntity = ecb.CreateEntity(entityInQueryIndex);
            ecb.AddComponent(entityInQueryIndex, orderEntity, new Order
            {
                ExecutorEntity = agentEntity,
                FacilityEntity = facilityEntity,
                OutputName = outputItemName,
                Amount = outputAmount
            });
            ecb.AddComponent(entityInQueryIndex, orderEntity, new OrderWatch
            {
                NodeEntity = nodeEntity,
                AgentEntity = agentEntity
            });
        }
    }
}