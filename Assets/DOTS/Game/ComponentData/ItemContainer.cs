using Unity.Entities;

namespace DOTS.Game.ComponentData
{
    public struct ItemContainer : IComponentData
    {
        /// <summary>
        /// 0表示容量不受限
        /// </summary>
        public int Capacity;

        /// <summary>
        /// 能否作为运输源
        /// </summary>
        public bool IsTransferSource;
    }
}