using Unity.Entities;
using Unity.Mathematics;

namespace Zephyr.GOAP.Struct
{
    public struct NodeTime
    {
        public Entity AgentEntity;
        public float3 EndPosition;
        public float TotalTime;
    }
}