using Unity.Entities;

namespace Zephyr.GOAP.Component
{
    /// <summary>
    /// 存放某个action node对应的delta state
    /// </summary>
    public struct DeltaStates : IComponentData
    {
        public Entity ActionNodeEntity;
    }
}