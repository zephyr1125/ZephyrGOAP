using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;

namespace Zephyr.GOAP.Sample.Game.Authoring
{
    [RequiresEntityConversion]
    public class TreeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;

        [ValueDropdown("FruitNames")]
        public string FruitName;

        public byte Amount;

        private static string[] FruitNames = {
            ItemNames.Instance().RawAppleName.ToString(),
            ItemNames.Instance().RawPeachName.ToString()
        };
        
        public void Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
#if UNITY_EDITOR
            dstManager.SetName(entity, Name);
#endif
            dstManager.AddComponentData(entity, 
                new RawSourceTrait{RawName = FruitName, InitialAmount =  Amount});
            
            //在RawSourceItemCreationSystem中生成物品与连接容器
        }
    }
}