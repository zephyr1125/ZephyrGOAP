using Unity.Entities;
using Unity.Mathematics;

namespace Zephyr.GOAP.Struct
{
    /// <summary>
    /// node上的agent信息（执行后）
    /// </summary>
    public struct NodeAgentInfo
    {
        public Entity AgentEntity;
        public float3 EndPosition;
        public float NavigateTime;
        public float ExecuteTime;
    }
}