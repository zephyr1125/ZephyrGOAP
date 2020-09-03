using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;

namespace Zephyr.GOAP.Sample.Game.System.OrderSystem.OrderExecuteSystem
{
    /// <summary>
    /// 执行订单食用
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class EatSystem : JobComponentSystem
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
            var actions = GetComponentDataFromEntity<EatAction>(true);
            var time = Time.ElapsedTime;
            
            //先走时间+播动画
            var initHandle = Entities.WithName("EatInitJob")
                .WithAll<EatOrder>()
                .WithAll<OrderReadyToExecute>()
                .WithReadOnly(actions)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order) =>
                {
                    Utils.OrderExecuteStart(order, actions, orderEntity, entityInQueryIndex, ecb, time);
                }).Schedule(inputDeps);
            
            //走完时间才正经执行
            var allItemRefs = GetBufferFromEntity<ContainedItemRef>(true);
            var allCounts = GetComponentDataFromEntity<Count>(true);
            var allStaminas = GetComponentDataFromEntity<Stamina>(true);
            
            var executeHandle = Entities
                .WithName("EatExecuteJob")
                .WithAll<EatOrder>()
                .WithAll<OrderExecuting>()
                .WithReadOnly(allItemRefs)
                .WithReadOnly(allCounts)
                .WithReadOnly(allStaminas)
                .ForEach((Entity orderEntity, int entityInQueryIndex, ref Order order, in OrderExecuteTime orderExecuteTime) =>
                {
                    if (time - orderExecuteTime.StartTime < orderExecuteTime.ExecutePeriod)
                        return;

                    var executorEntity = order.ExecutorEntity;
                    var itemName = order.ItemName;
                    var amount = order.Amount;
                    var containerEntity = order.FacilityEntity;
                    
                    //物品容器失去物品
                    var itemBuffer = allItemRefs[containerEntity];
                    Utils.ModifyItemInContainer(entityInQueryIndex, ecb, containerEntity, itemBuffer,
                        allCounts, itemName, -amount);
                    
                    //获得体力
                    var stamina = allStaminas[executorEntity];
                    //todo 正式游戏应当从食物数据中确认应该获得多少体力
                    stamina.Value += Utils.GetFoodStamina(itemName);
                    ecb.SetComponent(entityInQueryIndex, executorEntity, stamina);
                    
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