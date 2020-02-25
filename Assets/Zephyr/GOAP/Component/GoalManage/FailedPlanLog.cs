using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage
{
    /// <summary>
    /// 位于agent上的，失败规划的历史记录，定期清理
    /// </summary>
    public struct FailedPlanLog : IBufferElementData
    {
        public Entity GoalEntity;
        public float Time;
    }
}