using Unity.Entities;

namespace Zephyr.GOAP.Component
{
    /// <summary>
    /// 用于从node链接到goal
    /// </summary>
    public struct GoalRefForNode : IComponentData
    {
        public Entity GoalEntity;
    }
}