using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Zephyr.GOAP.Struct
{
    public struct StackData : IDisposable
    {
        public Entity AgentEntity;
        public float3 AgentPosition;

        public StateGroup CurrentStates;
        
        public void Dispose()
        {
            CurrentStates.Dispose();
        }
    }
}