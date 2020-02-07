using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;

namespace Zephyr.GOAP.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 0)]
    public class TreeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;

        [ValueDropdown("FruitNames")]
        public string FruitName;
        public int FruitAmount;
        
        private static string[] FruitNames = { "raw_apple", "raw_peach" };
        
        public void Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
#if UNITY_EDITOR
            dstManager.SetName(entity, Name);
#endif
            dstManager.AddComponentData(entity, new ItemContainerTrait());
            dstManager.AddComponentData(entity, 
                new ItemContainer{Capacity = 10, IsTransferSource = true});
            var buffer = dstManager.AddBuffer<ContainedItemRef>(entity);
            for (var i = 0; i < FruitAmount; i++)
            {
                buffer.Add(new ContainedItemRef{ItemName = FruitName});
            }
        }
    }
}