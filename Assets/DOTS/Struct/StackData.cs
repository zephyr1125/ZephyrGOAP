using System;
using Unity.Entities;

namespace DOTS.Struct
{
    public struct StackData : IDisposable
    {
        public Entity AgentEntity;
        
        public void Dispose()
        {
            
        }
    }
}