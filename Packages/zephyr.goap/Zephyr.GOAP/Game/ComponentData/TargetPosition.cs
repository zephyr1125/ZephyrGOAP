using Unity.Entities;
using Unity.Mathematics;

namespace Zephyr.GOAP.Game.ComponentData
{
    public struct TargetPosition : IComponentData
    {
        public float3 Value;
    }
}