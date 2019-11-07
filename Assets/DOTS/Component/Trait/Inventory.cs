using Unity.Entities;
using Zephyr.GOAP.Runtime.Component;

namespace DOTS.Component.Trait
{
    public struct Inventory : IBufferElementData, ITrait
    {
        public NativeString64 Name;
        public bool IsRaw;
    }
}