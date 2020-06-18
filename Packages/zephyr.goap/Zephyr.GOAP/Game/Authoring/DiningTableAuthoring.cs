using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;

namespace Zephyr.GOAP.Game.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 1)]
    public class DiningTableAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
#if UNITY_EDITOR
            dstManager.SetName(entity, Name);
#endif
            dstManager.AddComponentData(entity, new DiningTableTrait());
            dstManager.AddComponentData(entity, new ItemContainer{IsTransferSource = true});
            dstManager.AddBuffer<ContainedItemRef>(entity);        }
    }
}