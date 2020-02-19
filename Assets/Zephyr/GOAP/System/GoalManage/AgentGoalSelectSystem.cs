using Unity.Collections;
using Unity.Entities;
using UnityEngine.Assertions;
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
        private struct CollectAgentGoalsJob : IJobForEachWithEntity<Goal, AgentGoal>
        {
            [WriteOnly]
            public NativeMultiHashMap<Entity, Goal>.ParallelWriter AgentGoals;
            public void Execute(Entity entity, int jobId, ref Goal goal, ref AgentGoal agentGoal)
            {
                AgentGoals.Add(agentGoal.Agent, goal);
            }
        }
        
        protected override void OnUpdate()
        {
            //todo global goal应与agent goal一起排序并被选择
            
            var agentGoals = new NativeMultiHashMap<Entity, Goal>(512, Allocator.TempJob);
            var job = new CollectAgentGoalsJob {AgentGoals = agentGoals.AsParallelWriter()};
            job.Schedule(this).Complete();

            Entities.WithAll<NoGoal>().ForEach(
                (Entity entity, ref Agent agent, DynamicBuffer<State> states) =>
                {
                    Assert.AreEqual(0, agent.ExecutingNodeId);
                    Assert.AreEqual(0, states.Length);
                    
                    var myGoals = new NativeList<Goal>(Allocator.Temp);
                    var enumerator = agentGoals.GetValuesForKey(entity);
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        //todo 排除自己近期失败过的
                        
                        myGoals.Add(current);
                    }

                    if (myGoals.Length > 0)
                    {
                        //按优先级和时间排序
                        myGoals.Sort();
                    
                        //选取第一个作为自己当前goal
                        states.Add(myGoals[0].State);
                    
                        //此goal移除
                        EntityManager.DestroyEntity(myGoals[0].GoalEntity);
                        
                        //agent转换状态
                        Utils.NextAgentState<NoGoal, GoalPlanning>(entity, EntityManager, false);
                    }

                    enumerator.Dispose();
                    myGoals.Dispose();
                });

            agentGoals.Dispose();
        }
    }
}