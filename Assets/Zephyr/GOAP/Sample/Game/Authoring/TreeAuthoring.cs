using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Sample.Game.Component;
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
                new RawSourceTrait{RawName = FruitName});
            
            //生成物品与连接容器
            var itemEntity = dstManager.CreateEntity();
            dstManager.AddComponentData(itemEntity, new Item{});
            dstManager.AddComponentData(itemEntity, new Name{Value = FruitName});
            dstManager.AddComponentData(itemEntity, new Count{Value = Amount});
            
            dstManager.AddComponentData(entity, new ItemContainer {IsTransferSource = false});
            var buffer = dstManager.AddBuffer<ContainedItemRef>(entity);
            buffer.Add(new ContainedItemRef{ItemEntity = itemEntity, ItemName = FruitName});

        }
    }
}