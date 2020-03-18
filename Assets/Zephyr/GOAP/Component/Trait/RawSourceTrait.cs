using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Component.Trait
{
    /// <summary>
    /// 原料提供者Trait，专门表示是只有Collector才能够收集的
    /// </summary>
    public struct RawSourceTrait : IComponentData, ITrait
    {
        public NativeString64 RawName;
    }
}