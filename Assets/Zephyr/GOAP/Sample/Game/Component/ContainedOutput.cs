using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component
{
    public struct ContainedOutput : IBufferElementData
    {
        public NativeString32 ItemOutput;
    }
}