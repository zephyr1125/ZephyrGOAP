using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.Game.System
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
                .WithNone<OrderInited>()
                .WithReadOnly(actions)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order) =>
                {
                    var executorEntity = order.ExecutorEntity;
                    //获取执行时间
                    var setting = new State
                    {
                        Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                        ValueString = order.ItemName
                    };
                    var actionPeriod = actions[executorEntity].GetExecuteTime(setting);
                    
                    //todo 播放动态
                    
                    //初始化完毕
                    ecb.AddComponent(entityInQueryIndex, orderEntity,
                        new OrderInited{ExecutePeriod = actionPeriod, StartTime = time});
                }).Schedule(inputDeps);
            
            //走完时间才正经执行
            var allItemRefs = GetBufferFromEntity<ContainedItemRef>(true);
            var allCounts = GetComponentDataFromEntity<Count>(true);
            var stateBuffers = GetBufferFromEntity<State>(true);
            
            var executeHandle = Entities
                .WithName("CookExecuteJob")
                .WithAll<CookOrder>()
                .WithReadOnly(stateBuffers)
                .WithReadOnly(allItemRefs)
                .WithReadOnly(allCounts)
                .ForEach((Entity orderEntity, int entityInQueryIndex, ref Order order, in OrderInited orderInited) =>
                {
                     if (time - orderInited.StartTime < orderInited.ExecutePeriod)
                         return;
                    
                     // 查询配方，获取input
                     // todo 正式在查询配方时，不应基于AI所使用的BaseStates,而应该是来自外部数据
                     var outputFilter = new State
                     {
                         Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
                         ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                         ValueString = order.ItemName
                     };
                     
                     var baseStatesBuffer = stateBuffers[BaseStatesHelper.BaseStatesEntity];
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
                    
                     //移除OrderInited
                     ecb.RemoveComponent<OrderInited>(entityInQueryIndex, orderEntity);
                     
                     //order减小需求的数量
                     if (outputAmount > order.Amount) outputAmount = order.Amount;    //避免减小到负值，byte不支持
                     order.Amount -= outputAmount;
                     
                     baseStates.Dispose();
                     inputs.Dispose();
            }).Schedule(initHandle);
            ECBSystem.AddJobHandleForProducer(executeHandle);
            return executeHandle;
        }
    }
}