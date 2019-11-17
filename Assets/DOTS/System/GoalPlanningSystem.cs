using DOTS.Component;
using DOTS.Component.Actions;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class GoalPlanningSystem : JobComponentSystem
    {
        private EntityQuery _agentQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _agentQuery = GetEntityQuery(
                ComponentType.ReadOnly<Agent>(),
                ComponentType.ReadOnly<PlanningGoal>(),
                ComponentType.ReadOnly<Action>());
        }

        [RequireComponentTag(typeof(Agent))]
        public struct ExpandJob : IJobForEachWithEntity_EBC<Action, PlanningGoal>
        {
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Action> actionBuffer,
                ref PlanningGoal goalPlanning)
            {
                var stackData = new StackData
                {
                    AgentEntity = entity,
                    CurrentStates = new StateGroup(10, Allocator.Temp)
                };
                
                var uncheckedNodes = new NativeList<Node>(Allocator.Temp);
                var unexpandedNodes = new NativeList<Node>(Allocator.Temp);
                var expandedNodes = new NativeList<Node>(Allocator.Temp);
                
                //goalNode进入待展开列表
                unexpandedNodes.Add(goalPlanning.Goal);
                
                while (unexpandedNodes.Length>0)
                {
                    //待检查列表进行Sensor查询请求并把结果存入StackData
                    //全部待检查node的sensor请求一起缓存，并统一查询以提高效率
                    
                    
                    //对待检查列表进行检查并挑选进入待展开列表
                    //对待展开列表进行展开，并挑选进入待检查和展开后列表
                    //对展开后列表进行失败判定并清空
                    //直至待展开列表为空或Early Exit
                    
                }

                stackData.Dispose();
                uncheckedNodes.Dispose();
                unexpandedNodes.Dispose();
                expandedNodes.Dispose();
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}