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
                typeof(GlobalGoalPool), typeof(State), typeof(GoalPriority));
            AgentGoalPoolEntities = new NativeHashMap<Entity, Entity>(7, Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            Entities.WithAll<Agent>().WithNone<GoalPoolRef>().ForEach(entity =>
                {
                    var poolEntity = EntityManager.CreateEntity(
                        typeof(State), typeof(GoalPriority));
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
        /// <param name="priority"></param>
        public static void AddGlobalGoal(State state, Priority priority = Priority.Normal)
        {
            var bufferState = _entityManager.GetBuffer<State>(GlobalGoalPoolEntity);
            var bufferPriority = _entityManager.GetBuffer<GoalPriority>(GlobalGoalPoolEntity);

            bufferState.Add(state);
            bufferPriority.Add(new GoalPriority {Priority = priority});
        }
        
        /// <summary>
        /// For outside ECS World
        /// 如果state已存在，只调整优先级
        /// </summary>
        /// <param name="agentEntity"></param>
        /// <param name="state"></param>
        /// <param name="priority"></param>
        public static void AddAgentGoal(Entity agentEntity, State state, Priority priority = Priority.Normal)
        {
            var poolEntity = AgentGoalPoolEntities[agentEntity];
            var bufferState = _entityManager.GetBuffer<State>(poolEntity);
            var bufferPriority = _entityManager.GetBuffer<GoalPriority>(poolEntity);
            
            //如果已经存在，只调整优先级
            if (HasGoal(agentEntity, state, out var id, out var existPriority))
            {
                if (priority != existPriority)
                {
                    bufferPriority[id] = new GoalPriority{Priority = priority};
                }   
                return;
            }
            
            bufferState.Add(state);
            bufferPriority.Add(new GoalPriority {Priority = priority});
        }

        public static bool HasGoal(Entity agentEntity, State state, out int id, out Priority priority)
        {
            var poolEntity = AgentGoalPoolEntities[agentEntity];
            var bufferState = _entityManager.GetBuffer<State>(poolEntity);
            var bufferPriority = _entityManager.GetBuffer<GoalPriority>(poolEntity);

            for (var i = 0; i < bufferState.Length; i++)
            {
                if (bufferState[i].Equals(state))
                {
                    id = i;
                    priority = bufferPriority[i].Priority;
                    return true;
                }
            }

            id = -1;
            priority = Priority.None;
            return false;
        }
    }
}