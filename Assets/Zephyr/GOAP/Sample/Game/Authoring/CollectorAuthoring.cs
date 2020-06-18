using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Sample.Game.Component;

namespace Zephyr.GOAP.Sample.Game.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 1)]
    public class CollectorAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;
        
        public void Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
#if UNITY_EDITOR
            dstManager.SetName(entity, Name);
#endif
            dstManager.AddComponentData(entity, new CollectorTrait());
            dstManager.AddComponentData(entity, new ItemContainer{IsTransferSource = true});
            dstManager.AddBuffer<ContainedItemRef>(entity);
        }
    }
}