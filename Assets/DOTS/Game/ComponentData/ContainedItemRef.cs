using Unity.Entities;

namespace DOTS.Game.ComponentData
{
    public struct ContainedItemRef : IBufferElementData
    {
        public NativeString64 ItemName;
        public Entity ItemEntity;
    }
}