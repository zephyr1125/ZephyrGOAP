using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Game.ComponentData
{
    public struct ContainedOutput : IBufferElementData
    {
        public NativeString64 ItemOutput;
    }
}