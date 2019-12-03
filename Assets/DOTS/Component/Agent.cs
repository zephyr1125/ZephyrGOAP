using Unity.Entities;

namespace DOTS.Component
{
    public struct Agent : IComponentData
    {
        public GoapState GoapState;
        public int ExecutingNodeId;
    }
}