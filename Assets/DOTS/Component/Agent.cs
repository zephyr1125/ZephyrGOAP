using Unity.Entities;

namespace DOTS.Component
{
    public struct Agent : IComponentData
    {
        public int ExecutingNodeId;
    }
}