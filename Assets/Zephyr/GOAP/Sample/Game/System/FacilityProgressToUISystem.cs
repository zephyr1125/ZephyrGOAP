using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
using Zephyr.GOAP.Sample.Game.UI;

namespace Zephyr.GOAP.Sample.Game.System
{
    /// <summary>
    /// 把设施的生产进度发送到UI
    /// </summary>
    public class FacilityProgressToUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var time = Time.ElapsedTime;
            Entities
                .WithoutBurst()
                .ForEach((in Order order, in OrderExecuteTime orderExecuteTime) =>
            {
                var facilityEntity = order.FacilityEntity;
                var translations = GetComponentDataFromEntity<Translation>(true);
                var position = translations[facilityEntity].Value + new float3(0, -2, 0);
                var progress = (time - orderExecuteTime.StartTime) / orderExecuteTime.ExecutePeriod;
                FacilityProgressManager.Instance.UpdateFacilityProgress(facilityEntity, (float)progress, position);
            }).Run();
        }
    }
}