using Unity.Entities;
using Unity.Mathematics;

namespace Zephyr.GOAP.Component
{
    public struct TargetPosition : IComponentData
    {
        public float3 Value;
    }
}