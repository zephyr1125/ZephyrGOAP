using System;
using Unity.Entities;

namespace DOTS
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