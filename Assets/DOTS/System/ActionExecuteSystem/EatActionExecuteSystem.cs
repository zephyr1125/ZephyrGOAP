using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace DOTS.System.ActionExecuteSystem
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class EatActionExecuteSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        [RequireComponentTag(typeof(EatAction), typeof(ReadyToActing))]
        private struct ActionExecuteJob : IJobForEachWithEntity_EBBBCC<Node, State, ContainedItemRef,
            Agent, Stamina>
        {
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, DynamicBuffer<ContainedItemRef> containedItems,
                ref Agent agent, ref Stamina stamina)
            {
                //执行进度要处于正确的id上
                var currentNode = nodes[agent.ExecutingNodeId];
                if (!currentNode.Name.Equals(new NativeString64(nameof(EatAction))))
                    return;

                //从precondition里找食物.
                var targetItemName = new NativeString64();
                for (var i = 0; i < states.Length; i++)
                {
                    if ((currentNode.PreconditionsBitmask & (ulong) 1 << i) <= 0) continue;
                    var precondition = states[i];
                    Assert.AreEqual(entity, precondition.Target);

                    targetItemName = precondition.ValueString;
                    break;
                }
                
                //从自身找到物品引用，并移除
                for (var i = 0; i < containedItems.Length; i++)
                {
                    var containedItemRef = containedItems[i];
                    if (!containedItemRef.ItemName.Equals(targetItemName)) continue;
                    containedItems.RemoveAt(i);
                    break;
                }
                
                //获得体力
                //todo 正式游戏应当从食物数据中确认应该获得多少体力，并且由专用system负责吃的行为
                stamina.Value += 0.5f;
                
                //通知执行完毕
                Utils.NextAgentState<ReadyToActing, ReadyToNavigating>(
                    entity, jobIndex, ref ECBuffer, agent, true);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new ActionExecuteJob
            {
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var handle = job.Schedule(this, inputDeps);
            ECBSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}