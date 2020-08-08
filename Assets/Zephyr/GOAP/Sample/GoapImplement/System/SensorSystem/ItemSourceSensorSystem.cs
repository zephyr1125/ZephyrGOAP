using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System.SensorManage;

namespace Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem
{
    /// <summary>
    /// 探测所有可拿取的物品容器的物品情况
    /// todo 可以考虑下是否有必要优化为只记录离agent最近的，节省内存牺牲运算
    /// </summary>
    public class ItemSourceSensorSystem : SensorSystemBase
    {
        protected override JobHandle ScheduleSensorJob(JobHandle inputDeps,
            EntityCommandBuffer.ParallelWriter ecb, Entity baseStateEntity)
        {
            return Entities.WithAll<ItemContainerTrait>()
                .ForEach((Entity itemContainerEntity, int entityInQueryIndex,
                    DynamicBuffer<ContainedItemRef> itemRefs,
                    in ItemContainer itemContainer, in Translation translation) =>
                {
                    if (!itemContainer.IsTransferSource) return;

                    for (var i = 0; i < itemRefs.Length; i++)
                    {
                        var itemRef = itemRefs[i];
                        ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                        {
                            Target = itemContainerEntity,
                            Position = translation.Value,
                            Trait = TypeManager.GetTypeIndex<ItemContainerTrait>(),
                            ValueString = itemRef.ItemName
                        });
                    }
                }).Schedule(inputDeps);
        }
    }
}