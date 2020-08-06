using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Component.GoalManage;

namespace Zephyr.GOAP.System.ActionExecuteManage
{
    /// <summary>
    /// plan nodes的第一个(具有goal ref)启动时，通知到goal启动的时间
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class PlanStartTimeRecordSystem : SystemBase
    {
        private EntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var time = Time.ElapsedTime;
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var goals = GetComponentDataFromEntity<Goal>();
            var handle = Entities.WithName("PlanStartTimeRecordJob")
                .WithAll<Node, ActionNodeActing>()
                .WithNone<NodeStartTimeRecorded>()
                .WithReadOnly(goals)
                .ForEach((Entity nodeEntity, int entityInQueryIndex, in GoalRefForNode goalRef) =>
                {
                    var goal = goals[goalRef.GoalEntity];
                    goal.ExecuteStartTime = time;
                    ecb.SetComponent(entityInQueryIndex, goalRef.GoalEntity, goal);
                    ecb.AddComponent(entityInQueryIndex, nodeEntity, new NodeStartTimeRecorded());
                }).ScheduleParallel(Dependency);
            _ecbSystem.AddJobHandleForProducer(handle);
            Dependency = handle;
        }
    }
}