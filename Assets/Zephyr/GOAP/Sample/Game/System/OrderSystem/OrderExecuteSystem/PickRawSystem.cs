using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;

namespace Zephyr.GOAP.Sample.Game.System.OrderSystem.OrderExecuteSystem
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class PickRawSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = ECBSystem.CreateCommandBuffer().AsParallelWriter();
            var actions = GetComponentDataFromEntity<PickRawAction>(true);
            var time = Time.ElapsedTime;
            
            //先走时间+播动画
            var initHandle = Entities.WithName("PickRawInitJob")
                .WithAll<PickRawOrder>()
                .WithAll<OrderReadyToExecute>()
                .WithReadOnly(actions)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order) =>
                {
                    Utils.OrderExecuteStart(order, actions, orderEntity, entityInQueryIndex, ecb, time);
                }).Schedule(inputDeps);
            
            //走完时间才正经执行
            var allItemRefs = GetBufferFromEntity<ContainedItemRef>(true);
            var allCounts = GetComponentDataFromEntity<Count>(true);
            
            var executeHandle = Entities
                .WithName("PickRawExecuteJob")
                .WithAll<PickRawOrder>()
                .WithAll<OrderExecuting>()
                .WithReadOnly(allItemRefs)
                .WithReadOnly(allCounts)
                .ForEach((Entity orderEntity, int entityInQueryIndex, ref Order order, in OrderExecuteTime orderExecuteTime) =>
                {
                     if (time - orderExecuteTime.StartTime < orderExecuteTime.ExecutePeriod)
                         return;

                     var itemName = order.ItemName;
                     var amount = order.Amount;
                     
                     //执行者获得物品
                     var executorEntity = order.ExecutorEntity;
                     var executorBuffer = allItemRefs[executorEntity];
                     Utils.ModifyItemInContainer(entityInQueryIndex, ecb, executorEntity, executorBuffer,
                         allCounts, itemName, amount);

                     //原料容器失去物品
                     var rawEntity = order.FacilityEntity;
                     var rawBuffer = allItemRefs[rawEntity];
                     Utils.ModifyItemInContainer(entityInQueryIndex, ecb, rawEntity, rawBuffer,
                         allCounts, itemName, -amount);
                    
                     //order减小需求的数量
                     order.Amount -= amount;
                     
                     //下一阶段
                     Utils.NextOrderState<OrderExecuting, OrderReadyToNavigate>(orderEntity, entityInQueryIndex, ecb);
            }).Schedule(initHandle);
            ECBSystem.AddJobHandleForProducer(executeHandle);
            return executeHandle;
        }

        
    }
}