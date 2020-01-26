using Unity.Entities;
using UnityEngine;

namespace DOTS.System.SensorSystem
{
    /// <summary>
    /// 在这个ECB里把所有SensorSystem下达的设置指令实际填给CurrentState
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SensorSystemGroup))]
    [UpdateBefore(typeof(GoalPlanningSystem))]
    public class SensorsSetCurrentStatesECBufferSystem : EntityCommandBufferSystem
    {
        
    }
}