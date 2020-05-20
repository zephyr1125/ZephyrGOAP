using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage.GoalState;

namespace Zephyr.GOAP.System.ActionExecuteManage
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(ActionExecuteDoneSystem))]
    public class PlanExecutionDoneSystem : SystemBase
    {
        public EntityCommandBufferSystem EcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            EcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = EcbSystem.CreateCommandBuffer();
            Entities
                .WithoutBurst()
                .WithAll<ExecutingGoal>()
                .ForEach((Entity planEntity,
                DynamicBuffer<ActionNodeOfGoal> nodeRefs) =>
            {
                //全部关联node都不存在了，则此plan完成并删除
                for (var i = 0; i < nodeRefs.Length; i++)
                {
                    if (EntityManager.Exists(nodeRefs[i].ActionNodeEntity))
                    {
                        return;
                    }
                }
                ecb.DestroyEntity(planEntity);
            }).Run();
        }
    }
}