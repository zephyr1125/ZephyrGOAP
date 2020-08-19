using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component
{
    public struct Name : IComponentData
    {
        public FixedString32 Value;
    }
}