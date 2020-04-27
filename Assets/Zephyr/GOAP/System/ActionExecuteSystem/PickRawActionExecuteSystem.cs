// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using UnityEngine.Assertions;
// using Zephyr.GOAP.Action;
// using Zephyr.GOAP.Component;
// using Zephyr.GOAP.Component.AgentState;
// using Zephyr.GOAP.Game.ComponentData;
// using Zephyr.GOAP.Struct;
//
// namespace Zephyr.GOAP.System.ActionExecuteSystem
// {
//     [UpdateInGroup(typeof(SimulationSystemGroup))]
//     public class PickRawActionExecuteSystem : JobComponentSystem
//     {
//         public EntityCommandBufferSystem ECBSystem;
//             
//         protected override void OnCreate()
//         {
//             base.OnCreate();
//             ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//         }
//
//         // [BurstCompile]
//         [RequireComponentTag(typeof(PickRawAction), typeof(ContainedItemRef), typeof(ReadyToAct))]
//         public struct ActionExecuteJob : IJobForEachWithEntity_EBBC<Node, State, Agent>
//         {
//             [NativeDisableParallelForRestriction]
//             public BufferFromEntity<ContainedItemRef> AllContainedItemRefs;
//             
//             public EntityCommandBuffer.Concurrent ECBuffer;
//             
//             public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
//                 DynamicBuffer<State> states, ref Agent agent)
//             {
//                 //执行进度要处于正确的id上
//                 var currentNode = nodes[agent.ExecutingNodeId];
//                 if (!currentNode.Name.Equals(new NativeString64(nameof(PickRawAction))))
//                     return;
//                 
//                 //从precondition里找物品名.
//                 var targetItemName = new NativeString64();
//                 for (var i = 0; i < states.Length; i++)
//                 {
//                     if ((currentNode.PreconditionsBitmask & (ulong)1 << i) > 0)
//                     {
//                         var precondition = states[i];
//                         Assert.IsTrue(precondition.Target!=null);
//                         
//                         targetItemName = precondition.ValueString;
//                         break;
//                     }
//                 }
//                 //todo 目前原料源不使用物品容器，直接提供无限的原料物品
//
//                 //自己获得物品
//                 var buffer = AllContainedItemRefs[entity];
//                 buffer.Add(new ContainedItemRef{ItemName = targetItemName});
//                 
//                 //通知执行完毕
//                 Utils.NextAgentState<ReadyToAct, ReadyToNavigate>(
//                     entity, jobIndex, ref ECBuffer, agent, true);
//             }
//         }
//         
//         protected override JobHandle OnUpdate(JobHandle inputDeps)
//         {
//             var job = new ActionExecuteJob
//             {
//                 AllContainedItemRefs = GetBufferFromEntity<ContainedItemRef>(),
//                 ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
//             };
//             var handle = job.Schedule(this, inputDeps);
//             ECBSystem.AddJobHandleForProducer(handle);
//             return handle;
//         }
//     }
// }