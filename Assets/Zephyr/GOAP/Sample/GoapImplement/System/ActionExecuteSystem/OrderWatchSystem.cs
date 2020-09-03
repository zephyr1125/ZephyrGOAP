using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.GoapImplement.Component;

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
        /// 位于Order上表示被监控
        /// </summary>
        public struct OrderWatched : ISystemStateComponentData
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
            var allWatchingOrders = GetBufferFromEntity<WatchingOrder>(true);
            var handle = Entities.WithName("OrderWatchJob")
                .WithNone<Order>()
                .WithReadOnly(allWatchingOrders)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in OrderWatched orderWatch) =>
                {
                    var agentEntity = orderWatch.AgentEntity;
                    var nodeEntity = orderWatch.NodeEntity;

                    //移除当前watchingOrder
                    var watchingOrders = allWatchingOrders[agentEntity];
                    //因为ecb没有直接移除某buffer的办法，因此通过把不符合的重新赋值，留空符合的，这样来绕过
                    var buffer = ecb.SetBuffer<WatchingOrder>(entityInQueryIndex, agentEntity);
                    for (var watchingOrderId = 0; watchingOrderId < watchingOrders.Length; watchingOrderId++)
                    {
                        if (!watchingOrders[watchingOrderId].OrderEntity.Equals(orderEntity))
                        {
                            buffer.Add(watchingOrders[watchingOrderId]);
                        }
                    }

                    //如果这个order是watching的最后一个，则agent与node执行完毕
                    if (watchingOrders.Length == 1)
                    {
                        //通知执行完毕,注意此处默认agent应该是处于Acting状态了
                        Zephyr.GOAP.Utils.NextAgentState<Acting, ActDone>(agentEntity, entityInQueryIndex,
                            ecb, nodeEntity);
                    
                        //node指示执行完毕 
                        Zephyr.GOAP.Utils.NextActionNodeState<ActionNodeActing, ActionNodeDone>(nodeEntity,
                            entityInQueryIndex, ecb, agentEntity);
                    }

                    ecb.RemoveComponent<OrderWatched>(entityInQueryIndex, orderEntity);
                }).Schedule(Dependency);
            EcbSystem.AddJobHandleForProducer(handle);
        }
        
        public static Entity CreateOrderAndWatch<T>(EntityCommandBuffer.ParallelWriter ecb, int entityInQueryIndex,
            Entity agentEntity, Entity facilityEntity, FixedString32 outputItemName, int outputAmount,
            Entity nodeEntity, Entity dependentOrderEntity) where T: struct, IComponentData, IOrder
        {
            var orderEntity = ecb.CreateEntity(entityInQueryIndex);
            ecb.AddComponent(entityInQueryIndex, orderEntity, new Order
            {
                ExecutorEntity = agentEntity,
                FacilityEntity = facilityEntity,
                ItemName = outputItemName,
                Amount = outputAmount
            });
            ecb.AddComponent(entityInQueryIndex, orderEntity, new OrderWatched
            {
                NodeEntity = nodeEntity,
                AgentEntity = agentEntity
            });
            ecb.AddComponent<T>(entityInQueryIndex, orderEntity);
            ecb.AddComponent(entityInQueryIndex, orderEntity, new OrderReadyToNavigate());
            
            ecb.AppendToBuffer(entityInQueryIndex, agentEntity, new WatchingOrder{OrderEntity = orderEntity});

            if (dependentOrderEntity != Entity.Null)
            {
                ecb.AddComponent(entityInQueryIndex, orderEntity,
                    new DependentOrder{dependentOrderEntity = dependentOrderEntity});
            }

            return orderEntity;
        }
    }
}