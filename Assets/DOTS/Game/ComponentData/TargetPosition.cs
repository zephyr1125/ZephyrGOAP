using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Game.ComponentData
{
    public struct TargetPosition : IComponentData
    {
        public float3 Value;
    }
}