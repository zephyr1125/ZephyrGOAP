using System;
using Unity.Entities;

namespace DOTS.Game.ComponentData
{
    /// <summary>
    /// 最大移动速度
    /// </summary>
    [Serializable]
    public struct MaxMoveSpeed : IComponentData
    {
        public float value;
    }
}