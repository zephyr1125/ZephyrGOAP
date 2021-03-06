using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Sample.Game.Component;

namespace Zephyr.GOAP.Sample.Game.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class StaminaConsumeSystem : JobComponentSystem
    {
        // [BurstCompile]
        private struct StaminaConsumeJob : IJobForEach<Stamina>
        {
            public float DeltaTime;
            
            public void Execute(ref Stamina stamina)
            {
                var newStamina = stamina.Value + stamina.ChangeSpeed * DeltaTime;
                if (newStamina < 0) newStamina = 0;
                stamina.Value = newStamina;
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new StaminaConsumeJob
            {
                DeltaTime = Time.DeltaTime
            };
            return job.Schedule(this, inputDeps);
        }
    }
}