using Unity.Entities;

namespace DOTS.Game.ComponentData
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