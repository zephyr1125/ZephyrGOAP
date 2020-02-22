using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine.Assertions;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoalManage
{
    /// <summary>
    /// 在agent没有当前任务时执行
    /// 综合个人goal库与公共goal库中选择优先级最高和最早下达的一个goal来执行
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(AgentGoalMonitorSystemGroup))]
    public class AgentGoalSelectSystem : ComponentSystem
    {
        private EntityQuery _agentEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _agentEntityQuery = GetEntityQuery(typeof(Agent));
        }

        [ExcludeComponent(typeof(PlanningAgentRef))]
        private struct CollectAgentGoalsJob : IJobForEachWithEntity<Goal, AgentGoal>
        {
            [WriteOnly]
            public NativeMultiHashMap<Entity, Goal>.ParallelWriter AgentGoals;
            public void Execute(Entity entity, int jobId, ref Goal goal, ref AgentGoal agentGoal)
            {
                AgentGoals.Add(agentGoal.Agent, goal);
            }
        }

        /// <summary>
        /// 各个agent检查global goals里自己能直接执行的，计入列表
        /// “直接执行”的概念是自己有action能够以goal的state为目标
        /// </summary>
        [RequireComponentTag(typeof(GlobalGoal))]
        [ExcludeComponent(typeof(PlanningAgentRef))]
        private struct CollectGlobalGoalsJob<T> : IJobForEachWithEntity<Goal> where T : struct, IAction
        {
            [ReadOnly]
            public StackData StackData;
            
            [WriteOnly]
            public NativeMultiHashMap<Entity, Goal>.ParallelWriter AvailableGoals;

            public T Action;

            public void Execute(Entity entity, int index, ref Goal goal)
            {
                var goalStates = new StateGroup(1, Allocator.Temp);
                goalStates.Add(goal.State);
                
                var targetState = Action.GetTargetGoalState(ref goalStates, ref StackData);
                if (!targetState.Equals(State.Null))
                {
                    AvailableGoals.Add(StackData.AgentEntity, goal);
                }
                
                goalStates.Dispose();
            }
        }
        
        private JobHandle ScheduleCollectGlobalGoalsJob<T>(JobHandle dependHandle, EntityManager entityManager,
            ref StackData stackData, NativeMultiHashMap<Entity, Goal> availableGoals)
            where T : struct, IAction
        {
            if (entityManager.HasComponent<T>(stackData.AgentEntity))
            {
                dependHandle = new CollectGlobalGoalsJob<T>
                {
                    StackData = stackData,
                    AvailableGoals = availableGoals.AsParallelWriter(),
                    Action = new T(),
                }.Schedule(this, dependHandle);
            }

            return dependHandle;
        }

        private void CollectGlobalGoals(EntityManager entityManager,
            NativeMultiHashMap<Entity, Goal> availableGlobalGoals)
        {
            //从currentState的存储Entity上拿取current states
            var currentStateBuffer = EntityManager.GetBuffer<State>(CurrentStatesHelper.CurrentStatesEntity);
            var stackData = new StackData
            {
                CurrentStates = new StateGroup(ref currentStateBuffer, Allocator.TempJob)
            };
            
            var agentEntities = _agentEntityQuery.ToEntityArray(Allocator.TempJob);
            var handle = default(JobHandle);
            for (var i = 0; i < agentEntities.Length; i++)
            {
                var agentEntity = agentEntities[i];
                stackData.AgentEntity = agentEntity;
                stackData.AgentPosition =
                    EntityManager.GetComponentData<Translation>(agentEntity).Value;

                handle = ScheduleCollectGlobalGoalsJob<DropItemAction>(handle, entityManager,
                        ref stackData, availableGlobalGoals);
                handle = ScheduleCollectGlobalGoalsJob<PickItemAction>(handle, entityManager,
                        ref stackData, availableGlobalGoals);
                handle = ScheduleCollectGlobalGoalsJob<EatAction>(handle, entityManager,
                    ref stackData, availableGlobalGoals);
                handle = ScheduleCollectGlobalGoalsJob<CookAction>(handle, entityManager,
                    ref stackData, availableGlobalGoals);
                handle = ScheduleCollectGlobalGoalsJob<WanderAction>(handle, entityManager,
                    ref stackData, availableGlobalGoals);
            }
            handle.Complete();
            stackData.Dispose();
            agentEntities.Dispose();
        }
        
        protected override void OnUpdate()
        {
            //收集各agent各自的goal
            var agentGoals = new NativeMultiHashMap<Entity, Goal>(512, Allocator.TempJob);
            var job = new CollectAgentGoalsJob {AgentGoals = agentGoals.AsParallelWriter()};
            job.Schedule(this).Complete();
            
            //收集公共goal
            var availableGlobalGoals = new NativeMultiHashMap<Entity, Goal>(512, Allocator.TempJob);
            CollectGlobalGoals(EntityManager, availableGlobalGoals);

            Entities.WithAll<NoGoal>().ForEach(
                (Entity entity, ref Agent agent, DynamicBuffer<State> states) =>
                {
                    Assert.AreEqual(0, agent.ExecutingNodeId);
                    Assert.AreEqual(0, states.Length);
                    
                    var availableGoals = new NativeList<Goal>(Allocator.Temp);
                    
                    //加入我私有的goal
                    var enumerator = agentGoals.GetValuesForKey(entity);
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        //todo 排除自己近期失败过的
                        
                        availableGoals.Add(current);
                    }

                    //加入公共的goal
                    enumerator = availableGlobalGoals.GetValuesForKey(entity);
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        availableGoals.Add(current);
                    }

                    if (availableGoals.Length > 0)
                    {
                        //按优先级和时间排序
                        availableGoals.Sort();
                    
                        //选取第一个作为自己当前goal
                        states.Add(availableGoals[0].State);
                    
                        //此goal增加对本agent的引用
                        EntityManager.AddComponentData(availableGoals[0].GoalEntity,
                            new PlanningAgentRef{agentEntity = entity});
                        
                        //agent转换状态
                        Utils.NextAgentState<NoGoal, GoalPlanning>(entity, EntityManager, false);
                    }

                    enumerator.Dispose();
                    availableGoals.Dispose();
                });

            availableGlobalGoals.Dispose();
            agentGoals.Dispose();
        }
    }
}