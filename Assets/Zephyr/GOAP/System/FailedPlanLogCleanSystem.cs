using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component.GoalManage;

namespace Zephyr.GOAP.System
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class FailedPlanLogCleanSystem : JobComponentSystem
    {
        public float CoolDownTime = 5;
        public float UpdateIntervalTime = 1;

        private float _lastUpdateTime;
        
        private struct CleanJob : IJobForEach_B<FailedPlanLog>
        {
            public float CoolDownTime, CurrentTime;

            public void Execute(DynamicBuffer<FailedPlanLog> buffer)
            {
                for (var i = buffer.Length - 1; i >= 0; i--)
                {
                    var log = buffer[i];
                    if (!(CurrentTime - log.Time >= CoolDownTime)) continue;
                    buffer.RemoveAt(i);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var currentTime = (float)Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateIntervalTime) return inputDeps;

            _lastUpdateTime = currentTime;

            var job = new CleanJob
            {
                CoolDownTime = CoolDownTime,
                CurrentTime = currentTime
            };
            return job.Schedule(this, inputDeps);
        }
    }
}