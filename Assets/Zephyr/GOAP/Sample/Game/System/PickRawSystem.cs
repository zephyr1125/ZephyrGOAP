using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;

namespace Zephyr.GOAP.Sample.Game.System
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
            var pickRawActions = GetComponentDataFromEntity<PickRawAction>(true);
            var time = Time.ElapsedTime;
            
            //先走时间+播动画
            var initHandle = Entities.WithName("PickRawInitJob")
                .WithAll<PickRawOrder>()
                .WithNone<OrderInited>()
                .WithReadOnly(pickRawActions)
                .ForEach((Entity orderEntity, int entityInQueryIndex, in Order order) =>
                {
                    var executorEntity = order.ExecutorEntity;
                    //获取执行时间
                    var setting = new State();
                    var pickRawPeriod = pickRawActions[executorEntity].GetExecuteTime(setting);
                    
                    //todo 播放动态
                    
                    //初始化完毕
                    ecb.AddComponent(entityInQueryIndex, orderEntity,
                        new OrderInited{ExecutePeriod = pickRawPeriod, StartTime = time});
                }).Schedule(inputDeps);
            
            //走完时间才正经执行
            var allItemRefs = GetBufferFromEntity<ContainedItemRef>(true);
            var allCounts = GetComponentDataFromEntity<Count>(true);
            
            var executeHandle = Entities
                .WithName("PickRawExecuteJob")
                .WithAll<PickRawOrder>()
                .WithReadOnly(allItemRefs)
                .WithReadOnly(allCounts)
                .ForEach((Entity orderEntity, int entityInQueryIndex, ref Order order, in OrderInited orderInited) =>
                {
                     if (time - orderInited.StartTime < orderInited.ExecutePeriod)
                         return;

                     var itemName = order.OutputName;
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
                    
                     //移除OrderInited
                     ecb.RemoveComponent<OrderInited>(entityInQueryIndex, orderEntity);
                     
                     //order减小需求的数量
                     order.Amount -= amount;
            }).Schedule(initHandle);
            ECBSystem.AddJobHandleForProducer(executeHandle);
            return executeHandle;
        }

        
    }
}