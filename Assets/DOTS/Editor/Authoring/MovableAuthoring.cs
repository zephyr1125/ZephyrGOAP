using DOTS.Game.ComponentData;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 0)]
    public class MovableAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float MaxMoveSpeed;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new MaxMoveSpeed{value = MaxMoveSpeed});
        }
    }
}