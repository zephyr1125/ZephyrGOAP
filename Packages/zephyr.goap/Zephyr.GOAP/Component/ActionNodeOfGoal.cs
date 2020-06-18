using Unity.Entities;

namespace Zephyr.GOAP.Component
{
    /// <summary>
    /// 用于从goal链接到action node
    /// </summary>
    public struct ActionNodeOfGoal : IBufferElementData
    {
        public Entity ActionNodeEntity;
    }
}