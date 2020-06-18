using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage
{
    /// <summary>
    /// 位于goal上的，失败规划的历史记录，定期清理
    /// </summary>
    public struct FailedPlanLog : IBufferElementData
    {
        public float Time;
    }
}