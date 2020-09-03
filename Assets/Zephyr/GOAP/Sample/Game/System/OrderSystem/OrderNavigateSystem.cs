using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;

namespace Zephyr.GOAP.Sample.Game.System.OrderSystem
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class OrderNavigateSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = EcbSystem.CreateCommandBuffer().AsParallelWriter();
            var translations = GetComponentDataFromEntity<Translation>(true);
            var startHandle = Entities
                .WithAll<OrderReadyToNavigate>()
                .WithNone<DependentOrder>()
                .WithReadOnly(translations)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order) =>
                {
                    if (order.Amount <= 0) return;
                    
                    var executorEntity = order.ExecutorEntity;
                    var targetEntity = order.FacilityEntity;
                    
                    if (targetEntity == executorEntity || targetEntity==Entity.Null)
                    {
                        //目标为空或executor自身，无需移动，直接跳到ReadyToActing
                        Utils.NextOrderState<OrderReadyToNavigate, OrderReadyToExecute>(orderEntity, entityInQueryIndex, ecb);
                        return;
                    }
                    
                    //todo 路径规划
                
                    //设置target,通知开始移动
                    ecb.AddComponent(entityInQueryIndex, executorEntity,
                        new TargetPosition{Value = translations[targetEntity].Value});
                
                    //切换order状态,等待移动结束
                    Utils.NextOrderState<OrderReadyToNavigate, OrderNavigating>(orderEntity, entityInQueryIndex, ecb);
                    
                }).Schedule(inputDeps);

            var targetPositions = GetComponentDataFromEntity<TargetPosition>(true);
            var endHandle = Entities
                .WithAll<OrderNavigating>()
                .WithReadOnly(targetPositions)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order) =>
                {
                    //如果executor还在往目的地移动，则不继续
                    if (targetPositions.HasComponent(order.ExecutorEntity)) return;
                    
                    //切换order状态,可以进行Execute了
                    Utils.NextOrderState<OrderNavigating, OrderReadyToExecute>(orderEntity, entityInQueryIndex, ecb);
                }).Schedule(startHandle);
            
            EcbSystem.AddJobHandleForProducer(endHandle);
            return endHandle;
        }
    }
}