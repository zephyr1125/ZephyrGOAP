using Unity.Entities;

namespace Zephyr.GOAP.Sample.Game.Component
{
    public struct Stamina : IComponentData
    {
        /// <summary>
        /// Value per second
        /// </summary>
        public float ChangeSpeed;
        public float Value;
    }
}