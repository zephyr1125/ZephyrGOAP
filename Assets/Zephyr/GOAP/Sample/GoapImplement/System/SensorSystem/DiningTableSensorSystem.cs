using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System.SensorManage;

namespace Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem
{
    /// <summary>
    /// 检测世界里的DiningTable，写入其存在
    /// </summary>
    public class DiningTableSensorSystem : SensorSystemBase
    {
        protected override JobHandle ScheduleSensorJob(JobHandle inputDeps, EntityCommandBuffer.ParallelWriter ecb, Entity baseStateEntity)
        {
            return Entities.WithAll<DiningTableTrait>()
                .ForEach((Entity diningTableEntity, int entityInQueryIndex,
                    in Translation translation) =>
                {
                    //写入diningTable
                    ecb.AppendToBuffer(entityInQueryIndex, baseStateEntity, new State
                    {
                        Target = diningTableEntity,
                        Position = translation.Value,
                        Trait = TypeManager.GetTypeIndex<DiningTableTrait>(),
                    });
                }).Schedule(inputDeps);
        }
    }
}