using Unity.Entities;

namespace Zephyr.GOAP.System.SensorManage
{
    [UpdateInGroup(typeof (InitializationSystemGroup))]
    [UpdateAfter(typeof(SensorSystemGroup))]
    public class SensorECBSystem : EntityCommandBufferSystem
    {
    }
}