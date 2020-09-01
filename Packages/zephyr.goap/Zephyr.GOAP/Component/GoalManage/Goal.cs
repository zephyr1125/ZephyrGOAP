using System;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Component.GoalManage
{
    public struct Goal : IComponentData, IComparable<Goal>
    {
        public Entity GoalEntity;
        public State Require;
        public Priority Priority;
        public double CreateTime;
        /// <summary>
        /// 执行起始时间
        /// </summary>
        public double ExecuteStartTime;
        /// <summary>
        /// 预计执行时长
        /// </summary>
        public float EstimatePeriod;

        public int CompareTo(Goal other)
        {
            var priorityComparison = Priority.CompareTo(other.Priority);
            if (priorityComparison != 0) return priorityComparison;
            return CreateTime.CompareTo(other.CreateTime);
        }
    }
}