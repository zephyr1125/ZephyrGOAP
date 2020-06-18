using System;
using Unity.Entities;

namespace Zephyr.GOAP.Component
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