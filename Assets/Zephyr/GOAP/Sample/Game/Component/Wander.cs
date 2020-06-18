using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component
{
    public struct Wander : IComponentData
    {
        //持续时间
        public float Time;
    }
}