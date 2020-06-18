using Unity.Entities;

namespace Zephyr.GOAP.Game.ComponentData
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