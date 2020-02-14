using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoalManage
{
    /// <summary>
    /// Goal库管理
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(SensorSystemGroup))]
    public class GoalPoolHelper : ComponentSystem
    {
        public static Entity GlobalGoalPoolEntity;

        public static NativeHashMap<Entity, Entity> AgentGoalPoolEntities;

        private static EntityManager _entityManager;

        protected override void OnCreate()
        {
            _entityManager = EntityManager;
            GlobalGoalPoolEntity = EntityManager.CreateEntity(
                typeof(GlobalGoalPool), typeof(Goal));
            AgentGoalPoolEntities = new NativeHashMap<Entity, Entity>(7, Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Entities.WithAll<Agent>().WithNone<GoalPoolRef>().ForEach(entity =>
                {
                    var poolEntity = EntityManager.CreateEntity(typeof(Goal));
                    EntityManager.AddComponentData(poolEntity,
                        new AgentGoalPool{Agent = entity});

                    EntityManager.AddComponentData(entity,
                        new GoalPoolRef{GoalPoolEntity = poolEntity});
                    
                    AgentGoalPoolEntities.Add(entity, poolEntity);
                });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AgentGoalPoolEntities.Dispose();
        }

        /// <summary>
        /// For outside ECS World
        /// </summary>
        /// <param name="state"></param>
        /// <param name="time"></param>
        /// <param name="priority"></param>
        public static void AddGlobalGoal(State state, double time, Priority priority = Priority.Normal)
        {
            var bufferGoal = _entityManager.GetBuffer<Goal>(GlobalGoalPoolEntity);

            AddGoalToBuffer(bufferGoal, state, time, priority);
        }

        /// <summary>
        /// For outside ECS World
        /// 如果state已存在，只调整优先级
        /// </summary>
        /// <param name="agentEntity"></param>
        /// <param name="state"></param>
        /// <param name="time"></param>
        /// <param name="priority"></param>
        public static void AddAgentGoal(Entity agentEntity, State state, double time,
            Priority priority = Priority.Normal)
        {
            var poolEntity = AgentGoalPoolEntities[agentEntity];
            var bufferGoal = _entityManager.GetBuffer<Goal>(poolEntity);
            
            AddGoalToBuffer(bufferGoal, state, time, priority);
        }

        private static void AddGoalToBuffer(DynamicBuffer<Goal> bufferGoal, State state, double time,
            Priority priority)
        {
            //如果已经存在，只调整优先级
            if (HasGoal(bufferGoal, state, out var id, out var existPriority))
            {
                if (priority == existPriority) return;
                
                var goalExisted = bufferGoal[id];
                bufferGoal[id] = new Goal
                {
                    State = goalExisted.State,
                    Priority = priority,
                    CreateTime = goalExisted.CreateTime
                };
                return;
            }
            
            bufferGoal.Add(new Goal
            {
                State = state,
                Priority = priority,
                CreateTime = time
            });
        }

        private static bool HasGoal(DynamicBuffer<Goal> bufferGoal, State state, out int id, out Priority priority)
        {
            for (var i = 0; i < bufferGoal.Length; i++)
            {
                if (!bufferGoal[i].State.Equals(state)) continue;
                
                id = i;
                priority = bufferGoal[i].Priority;
                return true;
            }

            id = -1;
            priority = Priority.None;
            return false;
        }
    }
}