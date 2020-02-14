using System;
using System.Collections.Generic;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Component.GoalManage
{
    public struct Goal : IBufferElementData, IComparable<Goal>
    {
        public State State;
        public Priority Priority;
        public double CreateTime;

        public int CompareTo(Goal other)
        {
            var priorityComparison = Priority.CompareTo(other.Priority);
            if (priorityComparison != 0) return priorityComparison;
            return CreateTime.CompareTo(other.CreateTime);
        }
    }
}