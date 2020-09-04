using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.UI;

namespace Zephyr.GOAP.Sample.Game.System
{
    /// <summary>
    /// 把设施的生产进度发送到UI
    /// </summary>
    public class ItemContainerToUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var counts = GetComponentDataFromEntity<Count>(true); 
            Entities
                .WithoutBurst()
                .WithReadOnly(counts)
                .ForEach((Entity containerEntity, in DynamicBuffer<ContainedItemRef> containedItemRefs,
                    in Translation translation) =>
            {
                var itemNames = new NativeList<FixedString32>(Allocator.Temp);
                var itemCounts = new NativeList<byte>(Allocator.Temp);

                for (var itemId = 0; itemId < containedItemRefs.Length; itemId++)
                {
                    var itemCount = counts[containedItemRefs[itemId].ItemEntity];
                    itemNames.Add(containedItemRefs[itemId].ItemName);
                    itemCounts.Add(itemCount.Value);
                }

                var position = translation.Value + new float3(0, -5, 0);
                
                ItemContainerManager.Instance.UpdateItemContainer(containerEntity, itemNames, itemCounts, position);
            }).Run();
        }
    }
}