using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalManage.GoalState;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoalManage
{
    [UpdateInGroup(typeof(AgentGoalMonitorSystemGroup))]
    [AlwaysUpdateSystem]
    public abstract class AgentGoalMonitorComponentSystem : ComponentSystem
    {
        private float _timeInterval;
        private double _timeLastUpdate;

        private EntityQuery _agentGoalQuery;
        
        private NativeArray<Goal> _existedGoals;
        
        protected override void OnCreate()
        {
            _timeInterval = GetTimeInterval();
            _timeLastUpdate = Time.ElapsedTime;

            _agentGoalQuery = GetEntityQuery(typeof(Goal), typeof(AgentGoal));
        }

        protected override void OnUpdate()
        {
            var time = Time.ElapsedTime;
            if (time - _timeLastUpdate < _timeInterval) return;
            _timeLastUpdate = time;
            
            _existedGoals = _agentGoalQuery.ToComponentDataArray<Goal>(Allocator.TempJob);
            OnMonitorUpdate();
            _existedGoals.Dispose();
        }

        protected abstract float GetTimeInterval();

        protected abstract void OnMonitorUpdate();

        protected void AddGoal(Entity agentEntity, State state, double time,
            Priority priority = Priority.Normal)
        {
            //如果已经存在，只调整优先级
            if (HasGoal(state, out var id, out var existPriority))
            {
                if (priority == existPriority) return;
                
                var goalExisted = _existedGoals[id];
                goalExisted.Priority = priority;
                _existedGoals[id] = goalExisted;
                
                return;
            }

            var newGoalEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(newGoalEntity,
                new AgentGoal{Agent = agentEntity});
            EntityManager.AddComponentData(newGoalEntity, new Goal
                {
                    GoalEntity = newGoalEntity,
                    State = state,
                    Priority = priority,
                    CreateTime = time
                });
            EntityManager.AddComponentData(newGoalEntity, new IdleGoal
            {
                Time = (float)time
            });
        }

        private bool HasGoal(State state, out int id, out Priority priority)
        {
            for (var i = 0; i < _existedGoals.Length; i++)
            {
                if (!_existedGoals[i].State.Equals(state)) continue;

                id = i;
                priority = _existedGoals[i].Priority;
                return true;
            }

            id = -1;
            priority = Priority.None;
            return false;
        }
    }
}