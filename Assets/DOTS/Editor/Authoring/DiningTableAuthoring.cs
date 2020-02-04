using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 0)]
    public class DiningTableAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.SetName(entity, Name);
            dstManager.AddComponentData(entity, new DiningTableTrait());
        }
    }
}