using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System.GoalManage;

namespace Zephyr.GOAP.Sample.Game.System.AgentGoalMonitor
{
    public class WanderGoalMonitorSystem : AgentGoalMonitorSystemBase
    {
        private const float StaminaThreshold = 0.5f;
        
        protected override float GetTimeInterval()
        {
            return GOAP.Utils.GoalMonitorSystemInterval;
        }

        protected override void OnMonitorUpdate()
        {
            Entities.WithAll<Agent, WanderAction>().ForEach(
                (Entity entity, ref Stamina stamina)=>
            {
                if (stamina.Value < StaminaThreshold) return;
                
                AddGoal(entity, new State
                {
                    Target = entity,
                    Trait = TypeManager.GetTypeIndex<WanderTrait>(),
                }, Time.ElapsedTime);
            });
        }
    }
}