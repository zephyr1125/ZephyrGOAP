using DOTS.Game.ComponentData;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.Game.System
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class StaminaConsumeSystem : JobComponentSystem
    {
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
                DeltaTime = GameTime.Instance().DeltaSecond
            };
            return job.Schedule(this, inputDeps);
        }
    }
}