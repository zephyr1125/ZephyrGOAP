using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage
{
    /// <summary>
    /// 公共和私人Goal库使用的，与States并列，表达各个goal的优先级
    /// </summary>
    public struct GoalPriority : IBufferElementData
    {
        /// <summary>
        /// 0-4, 4 is highest priority
        /// </summary>
        public Priority Priority;
    }
}