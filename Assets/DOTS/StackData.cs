using System;
using Unity.Entities;

namespace DOTS
{
    public struct StackData : IDisposable
    {
        public Entity AgentEntity;
        public StateGroup Settings;
        
        public void Dispose()
        {
            Settings.Dispose();
        }
    }
}