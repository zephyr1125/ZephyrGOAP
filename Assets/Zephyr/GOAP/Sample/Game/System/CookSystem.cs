using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.Game.System
{
    /// <summary>
    /// 执行订单生产
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class CookSystem : SystemBase
    {
        public struct OrderInited : ISystemStateComponentData
        {
            public float ExecutePeriod;
            public double StartTime;
        }
        
        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = ECBSystem.CreateCommandBuffer().AsParallelWriter();
            var cookActions = GetComponentDataFromEntity<CookAction>(true);
            var time = Time.ElapsedTime;
            
            //先走时间+播动画
            var initHandle = Entities.WithName("CookInitJob")
                .WithAll<CookOrder>()
                .WithNone<OrderInited>()
                .WithReadOnly(cookActions)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order) =>
                {
                    var executorEntity = order.ExecutorEntity;
                    //获取执行时间
                    var setting = new State
                    {
                        Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                        ValueString = order.OutputName
                    };
                    var cookPeriod = cookActions[executorEntity].GetExecuteTime(setting);
                    
                    //todo 播放动态
                    
                    //初始化完毕
                    ecb.AddComponent(entityInQueryIndex, orderEntity,
                        new OrderInited{ExecutePeriod = cookPeriod, StartTime = time});
                }).ScheduleParallel(Dependency);
            ECBSystem.AddJobHandleForProducer(initHandle);
            
            //走完时间才正经执行
            // var baseStates = BaseStatesHelper.GetBaseStates(EntityManager, Allocator.TempJob);
            // var allItemRefs = GetBufferFromEntity<ContainedItemRef>(true);
            // var allCounts = GetComponentDataFromEntity<Count>();
            // Entities
            //     .WithName("CookExecuteJob")
            //     .WithAll<CookOrder>()
            //     // .WithDisposeOnCompletion(baseStates)
            //     .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order, in OrderInited orderInited) =>
            //     {
            //         if (time - orderInited.StartTime < orderInited.ExecutePeriod)
            //             return;
            //         
            //         //查询配方，获取input
            //         //todo 正式在查询配方时，不应基于AI所使用的BaseStates,而应该是来自外部数据
            //         var outputFilter = new State
            //         {
            //             Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
            //             ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
            //             ValueString = order.OutputName
            //         };
            //         // var inputs =
            //             // Utils.GetRecipeInputInStateGroup(baseStates, outputFilter, Allocator.Temp, out var outputAmount);
            //         //
            //         // //从cooker容器找到原料物品引用，并移除相应数目
            //         // var itemsInCooker = allItemRefs[order.FacilityEntity];
            //         // for (var inputId = 0; inputId < inputs.Length(); inputId++)
            //         // {
            //         //     var input = inputs[inputId];
            //         //     for (var itemRefId = itemsInCooker.Length - 1; itemRefId >= 0; itemRefId--)
            //         //     {
            //         //         var containedItemRef = itemsInCooker[itemRefId];
            //         //         if (!containedItemRef.ItemName.Equals(input.ValueString)) continue;
            //         //         //移去相应数量
            //         //         var valueBefore = allCounts[containedItemRef.ItemEntity].Value;
            //         //         allCounts[containedItemRef.ItemEntity] = new Count{Value = valueBefore-input.Amount};
            //         //         break;
            //         //     }
            //         // }
            //
            //         //cooker容器获得产物
            //         // var outputItemEntity = ecb.CreateEntity(entityInQueryIndex);
            //         // ecb.AddComponent(entityInQueryIndex, outputItemEntity, new Item());
            //         // // ecb.AddComponent(entityInQueryIndex, outputItemEntity, new Count{Value = outputAmount});
            //         // ecb.AppendToBuffer(entityInQueryIndex, order.FacilityEntity,
            //         //     new ContainedItemRef {ItemName = order.OutputName, ItemEntity = outputItemEntity});
            //         
            //         //移除StateComponent
            //         ecb.RemoveComponent<OrderInited>(entityInQueryIndex, orderEntity);
            //     }).ScheduleParallel(handleInit);
        }
    }
}