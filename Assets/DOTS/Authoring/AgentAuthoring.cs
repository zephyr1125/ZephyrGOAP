using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    [RequiresEntityConversion]
    [ConverterVersion("Zephyr", 1)]
    public class AgentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;
        public float InitialStamina;
        public float StaminaChangeSpeed;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.SetName(entity, Name);
            dstManager.AddComponentData(entity, new Stamina{
                Value = InitialStamina, ChangeSpeed = StaminaChangeSpeed});
            
            dstManager.AddComponentData(entity, new Agent());
            dstManager.AddComponentData(entity, new NoGoal());
            dstManager.AddBuffer<State>(entity);
            
            dstManager.AddComponentData(entity, new PickItemAction());
            dstManager.AddComponentData(entity, new DropItemAction());
            dstManager.AddComponentData(entity, new EatAction());
            dstManager.AddComponentData(entity, new CookAction());
            dstManager.AddComponentData(entity, new WanderAction());
        }
    }
}