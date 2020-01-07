using DOTS.Game.ComponentData;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
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
            dstManager.SetName(entity, Name);
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