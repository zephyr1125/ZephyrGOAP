using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component
{
    public struct ContainedOutput : IBufferElementData
    {
        public FixedString32 ItemOutput;
    }
}