using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Game.ComponentData
{
    public struct ContainedItemRef : IBufferElementData
    {
        public NativeString64 ItemName;
        public Entity ItemEntity;
    }
}