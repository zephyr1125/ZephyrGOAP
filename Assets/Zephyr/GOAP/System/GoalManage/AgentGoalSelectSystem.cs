using Unity.Collections;
using Unity.Entities;
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
    public class AgentGoalSelectSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<NoGoal>().ForEach(
                (Entity entity, ref Agent agent, DynamicBuffer<State> states) =>
                {
                    var globalGoals =
                        EntityManager.GetBuffer<Goal>(GoalPoolHelper.GlobalGoalPoolEntity).ToNativeArray(Allocator.Temp);
                    
                    var poolEntity = GoalPoolHelper.AgentGoalPoolEntities[entity];
                    var agentGoals = EntityManager.GetBuffer<Goal>(poolEntity)
                        .ToNativeArray(Allocator.Temp);
                    
                    //自体goal与公共goal合并
                    var allGoals = new NativeList<Goal>();
                    allGoals.AddRange(globalGoals);
                    allGoals.AddRange(agentGoals);
                    
                    //排除自己近期失败过的
                    
                    //按优先级和时间排序
                    allGoals.Sort();
                    
                    //选取第一个作为自己当前goal
                    
                    //在库中打上

                    agentGoals.Dispose();
                    globalGoals.Dispose();
                });
        }
    }
}