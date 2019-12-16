using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System.SensorSystem
{
    /// <summary>
    /// 探测所有可拿取的物品容器的物品情况
    /// todo 可以考虑下是否有必要优化为只记录离agent最近的，节省内存牺牲运算
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class ItemSourceSensorSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBufferSystem;

        protected override void OnCreate()
        {
            ECBufferSystem = World.GetOrCreateSystem<SensorsSetCurrentStatesECBufferSystem>();
        }

        [RequireComponentTag(typeof(ItemContainerTrait))]
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
                    var buffer = ECBuffer.SetBuffer<State>(jobIndex, CurrentStatesEntity);
                    buffer.Add(new State
                    {
                        Target = entity,
                        Trait = typeof(ItemContainerTrait),
                        ValueString = itemRef.ItemName
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