using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Game.ComponentData
{
    public struct ContainedItemRef : IBufferElementData
    {
        public NativeString32 ItemName;
        public Entity ItemEntity;
    }
}