using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage
{
    /// <summary>
    /// agent entity对自身的goal pool的引用
    /// </summary>
    public struct GoalPoolRef : IComponentData
    {
        public Entity GoalPoolEntity;
    }
}