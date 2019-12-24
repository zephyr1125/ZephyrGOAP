using Unity.Collections;
using Unity.Entities;

namespace DOTS.Game.ComponentData
{
    public struct ContainedOutput : IBufferElementData
    {
        public NativeString64 ItemOutput;
    }
}