using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.Game.System.OrderSystem.OrderExecuteSystem
{
    /// <summary>
    /// 执行订单生产
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class CookSystem : JobComponentSystem
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
            var actions = GetComponentDataFromEntity<CookAction>(true);
            var time = Time.ElapsedTime;
            
            //先走时间+播动画
            var initHandle = Entities.WithName("CookInitJob")
                .WithAll<CookOrder>()
                .WithAll<OrderReadyToExecute>()
                .WithReadOnly(actions)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order) =>
                {
                    Utils.OrderExecuteStart(order, actions, orderEntity, entityInQueryIndex, ecb, time);
                }).Schedule(inputDeps);
            
            //走完时间才正经执行
            var allItemRefs = GetBufferFromEntity<ContainedItemRef>(true);
            var allCounts = GetComponentDataFromEntity<Count>(true);
            var stateBuffers = GetBufferFromEntity<State>(true);
            var baseStateEntity = BaseStatesHelper.BaseStatesEntity;
            
            var executeHandle = Entities
                .WithName("CookExecuteJob")
                .WithAll<CookOrder>()
                .WithAll<OrderExecuting>()
                .WithReadOnly(stateBuffers)
                .WithReadOnly(allItemRefs)
                .WithReadOnly(allCounts)
                .ForEach((Entity orderEntity, int entityInQueryIndex, ref Order order, in OrderExecuteTime orderExecuteTime) =>
                {
                     if (time - orderExecuteTime.StartTime < orderExecuteTime.ExecutePeriod)
                         return;
                    
                     // 查询配方，获取input
                     // todo 正式在查询配方时，不应基于AI所使用的BaseStates,而应该是来自外部数据
                     var outputFilter = new State
                     {
                         Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
                         ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                         ValueString = order.ItemName
                     };
                     
                     var baseStatesBuffer = stateBuffers[baseStateEntity];
                     var baseStates = new StateGroup(baseStatesBuffer, Allocator.Temp);
                     var inputs =
                         Utils.GetRecipeInputInStateGroup(baseStates, outputFilter, Allocator.Temp, out var outputAmount);
                     
                    
                     //从cooker容器找到原料物品引用，并移除相应数目
                     var facilityEntity = order.FacilityEntity;
                     var itemsInCooker = allItemRefs[order.FacilityEntity];
                     for (var inputId = 0; inputId < inputs.Length(); inputId++)
                     {
                         var input = inputs[inputId];
                         var inputName = input.ValueString;
                         var inputAmount = input.Amount;
                         Utils.ModifyItemInContainer(entityInQueryIndex, ecb, facilityEntity,
                             itemsInCooker, allCounts, inputName, -inputAmount);
                     }
                    
                     //cooker容器获得产物
                     Utils.ModifyItemInContainer(entityInQueryIndex, ecb, facilityEntity,
                         itemsInCooker, allCounts, order.ItemName, outputAmount);
                     
                     //order减小需求的数量
                     order.Amount -= outputAmount;
                    
                     //下一阶段
                     Utils.NextOrderState<OrderExecuting, OrderReadyToNavigate>(orderEntity, entityInQueryIndex, ecb);

                     baseStates.Dispose();
                     inputs.Dispose();
            }).Schedule(initHandle);
            ECBSystem.AddJobHandleForProducer(executeHandle);
            return executeHandle;
        }
    }
}