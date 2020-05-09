using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Game.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 6)]
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
            dstManager.AddBuffer<State>(entity);
            
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

            dstManager.AddBuffer<FailedPlanLog>(entity);
        }
    }
}