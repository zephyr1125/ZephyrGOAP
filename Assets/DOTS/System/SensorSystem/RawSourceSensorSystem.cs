using DOTS.Component.Trait;
using DOTS.GameData.ComponentData;
using DOTS.Struct;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System.SensorSystem
{
    /// <summary>
    /// 探测所有原料来源的物品情况
    /// 原料来源的概念是容器是运输源&&容器含有原料
    /// todo 可以考虑下是否有必要优化为只记录离agent最近的，节省内存牺牲运算
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class RawSourceSensorSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBufferSystem;

        protected override void OnCreate()
        {
            ECBufferSystem = World.GetOrCreateSystem<SensorsSetCurrentStatesECBufferSystem>();
        }

        [RequireComponentTag(typeof(RawTrait))]
        private struct SenseJob : IJobForEachWithEntity_EBC<ContainedItemRef, ItemContainer>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;

            public Entity CurrentStatesEntity;
            
            public void Execute(Entity entity, int jobIndex, 
                DynamicBuffer<ContainedItemRef> itemRefs, ref ItemContainer container)
            {
                if (!container.IsTransferSource) return;

                foreach (var itemRef in itemRefs)
                {
                    //todo 应当判断只有是raw的item才算数
                    var buffer = ECBuffer.SetBuffer<State>(jobIndex, CurrentStatesEntity);
                    buffer.Add(new State
                    {
                        Target = entity,
                        Trait = typeof(RawTrait),
                        IsPositive = true,
                        Value = itemRef.ItemName
                    });
                }
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new SenseJob
            {
                ECBuffer = ECBufferSystem.CreateCommandBuffer().ToConcurrent(),
                CurrentStatesEntity = CurrentStatesHelper.CurrentStatesEntity
            };
            var handle = job.Schedule(this, inputDeps);
            ECBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}