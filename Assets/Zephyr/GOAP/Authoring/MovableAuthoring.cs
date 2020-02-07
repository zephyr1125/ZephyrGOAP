using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Game.ComponentData;

namespace Zephyr.GOAP.Authoring
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