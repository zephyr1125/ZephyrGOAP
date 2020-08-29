using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Sample.GoapImplement.System.ActionExecuteSystem;

namespace Zephyr.GOAP.Sample.Game.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 8)]
    public class AgentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;
        public float InitialStamina;
        public float StaminaChangeSpeed;

        public int CookLevel, CollectLevel;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
#if UNITY_EDITOR
            dstManager.SetName(entity, Name);
#endif
            dstManager.AddComponentData(entity, new Stamina{
                Value = InitialStamina, ChangeSpeed = StaminaChangeSpeed});
            
            dstManager.AddComponentData(entity, new Agent());
            dstManager.AddComponentData(entity, new Idle());
            
            dstManager.AddComponentData(entity, new PickItemAction());
            dstManager.AddComponentData(entity, new DropItemAction());
            dstManager.AddComponentData(entity, new PickRawAction());
            dstManager.AddComponentData(entity, new DropRawAction());
            dstManager.AddComponentData(entity, new EatAction());
            dstManager.AddComponentData(entity, new CookAction{Level = CookLevel});
            dstManager.AddComponentData(entity, new WanderAction());
            dstManager.AddComponentData(entity, new CollectAction{Level = CollectLevel});
            
            dstManager.AddComponentData(entity, new ItemContainerTrait());
            dstManager.AddComponentData(entity, 
                new ItemContainer{Capacity = 99, IsTransferSource = false});
            var buffer = dstManager.AddBuffer<ContainedItemRef>(entity);

            dstManager.AddBuffer<WatchingOrder>(entity);
        }
    }
}