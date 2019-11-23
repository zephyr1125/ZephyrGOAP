using Unity.Entities;

namespace DOTS.GameData.ComponentData
{
    public struct ContainedItemRef : IBufferElementData
    {
        public NativeString64 ItemName;
        public Entity ItemEntity;
    }
}