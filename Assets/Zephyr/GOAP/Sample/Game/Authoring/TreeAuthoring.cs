using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;

namespace Zephyr.GOAP.Sample.Game.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 0)]
    public class TreeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;

        [ValueDropdown("FruitNames")]
        public string FruitName;

        private static string[] FruitNames = { "raw_apple", Utils.RawPeachName.ToString() };
        
        public void Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
#if UNITY_EDITOR
            dstManager.SetName(entity, Name);
#endif
            dstManager.AddComponentData(entity, new RawSourceTrait{RawName = FruitName});
        }
    }
}