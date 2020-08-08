using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System.SensorManage;

namespace Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem
{
    /// <summary>
    /// 检测世界里的Cooker，写入其存在
    /// </summary>
    public class CookerSensorSystem : SensorSystemBase
    {
        protected override JobHandle ScheduleSensorJob(JobHandle inputDeps, EntityCommandBuffer.ParallelWriter ecb, Entity baseStateEntity)
        {
            return Entities.WithAll<CookerTrait>()
                .ForEach((Entity cookerEntity, int entityInQueryIndex,
                    DynamicBuffer<ContainedOutput> recipes, in Translation translation) =>
                {
                    //写入cooker
                    ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                    {
                        Target = cookerEntity,
                        Position = translation.Value,
                        Trait = TypeManager.GetTypeIndex<CookerTrait>(),
                    });
                }).Schedule(inputDeps);
        }
    }
}