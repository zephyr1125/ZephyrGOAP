using DOTS.Component;
using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    public class CampfireAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public string Name;
        [ValueDropdown("OutputNames")]
        public string[] Outputs;
        
        private static string[] OutputNames = { "roast_apple", "roast_peach" };
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.SetName(entity, Name);
            dstManager.AddComponentData(entity, new CookerTrait());
            var buffer = dstManager.AddBuffer<ContainedOutput>(entity);
            foreach (var output in Outputs)
            {
                buffer.Add(new ContainedOutput {ItemOutput = output});
            }
        }
    }
}