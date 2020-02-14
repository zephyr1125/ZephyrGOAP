using Unity.Entities;

namespace Zephyr.GOAP.System.GoalManage
{
    [UpdateInGroup(typeof(AgentGoalMonitorSystemGroup))]
    public abstract class AgentGoalMonitorComponentSystem : ComponentSystem
    {
        private float _timeInterval;
        private double _timeLastUpdate;
        
        protected override void OnCreate()
        {
            _timeInterval = GetTimeInterval();
            _timeLastUpdate = Time.ElapsedTime;
        }

        protected override void OnUpdate()
        {
            if (Time.ElapsedTime - _timeLastUpdate < _timeInterval) return;
            OnMonitorUpdate();
        }

        protected abstract float GetTimeInterval();

        protected abstract void OnMonitorUpdate();
    }
}