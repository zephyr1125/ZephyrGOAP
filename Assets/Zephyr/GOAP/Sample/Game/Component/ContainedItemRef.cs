using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component
{
    public struct ContainedItemRef : IBufferElementData
    {
        public FixedString32 ItemName;
        public Entity ItemEntity;
    }
}