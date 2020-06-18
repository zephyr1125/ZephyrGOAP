using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage
{
    /// <summary>
    /// 位于agent上的，链接到其正在plan/execute的goal
    /// </summary>
    public struct CurrentGoal : IComponentData
    {
        public Entity GoalEntity;
    }
}