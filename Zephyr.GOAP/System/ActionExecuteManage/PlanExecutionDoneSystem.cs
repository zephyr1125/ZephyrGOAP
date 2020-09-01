using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.GoalManage;
using Zephyr.GOAP.Component.GoalState;

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
                DynamicBuffer<ActionNodeOfGoal> nodeRefs, in Goal goal) =>
            {
                //全部关联node都不存在了，则此plan完成并删除
                for (var i = 0; i < nodeRefs.Length; i++)
                {
                    if (EntityManager.Exists(nodeRefs[i].ActionNodeEntity))
                    {
                        return;
                    }
                }
                //通知一下执行总共花掉的时间
                var executeTime = Time.ElapsedTime - goal.ExecuteStartTime;
                Debug.Log($"Plan done : {executeTime}[{goal.EstimatePeriod}]");
                ecb.DestroyEntity(planEntity);
            }).Run();
        }
    }
}