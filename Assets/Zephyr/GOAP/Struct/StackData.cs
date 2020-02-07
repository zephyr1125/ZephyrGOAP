using System;
using Unity.Entities;

namespace Zephyr.GOAP.Struct
{
    public struct StackData : IDisposable
    {
        public Entity AgentEntity;

        public StateGroup CurrentStates;
        
        public void Dispose()
        {
            CurrentStates.Dispose();
        }
    }
}