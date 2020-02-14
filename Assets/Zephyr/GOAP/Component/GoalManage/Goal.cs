using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Component.GoalManage
{
    public struct Goal : IBufferElementData
    {
        public State State;
        public Priority Priority;
        public double CreateTime;
    }
}