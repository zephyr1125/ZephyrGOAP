using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;

namespace Zephyr.GOAP.Sample.GoapImplement.Component.Trait
{
    /// <summary>
    /// 原料提供者Trait，专门表示是只有Collector才能够收集的
    /// </summary>
    public struct RawSourceTrait : IComponentData, ITrait
    {
        public FixedString32 RawName;
        public byte InitialAmount;
    }
}