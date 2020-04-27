// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using UnityEngine.Assertions;
// using Zephyr.GOAP.Action;
// using Zephyr.GOAP.Component;
// using Zephyr.GOAP.Component.AgentState;
// using Zephyr.GOAP.Component.Trait;
// using Zephyr.GOAP.Game.ComponentData;
// using Zephyr.GOAP.Struct;
//
// namespace Zephyr.GOAP.System.ActionExecuteSystem
// {
//     [UpdateInGroup(typeof(SimulationSystemGroup))]
//     public class CollectActionExecuteSystem : JobComponentSystem
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
//         [RequireComponentTag(typeof(CollectAction), typeof(ReadyToAct))]
//         private struct ActionExecuteJob : IJobForEachWithEntity_EBBCCC<State, ContainedItemRef, Node,
//             Agent, Stamina>
//         {
//             public EntityCommandBuffer.Concurrent ECBuffer;
//             
//             public void Execute(Entity entity, int jobIndex, DynamicBuffer<State> states,
//                 DynamicBuffer<ContainedItemRef> containedItems, ref Node node,
//                 ref Agent agent, ref Stamina stamina)
//             {
//                 //执行进度要处于正确的id上
//                 var currentNode = nodes[agent.ExecutingNodeId];
//                 if (!currentNode.Name.Equals(new NativeString64(nameof(CollectAction))))
//                     return;
//
//                 //CollectAction现在没有什么具体要做的事情，因为DropRawAction已经把物品放进去了
//                 //而后续也有PickItemAction处理
//                 
//                 //通知执行完毕
//                 Utils.NextAgentState<ReadyToAct, ReadyToNavigate>(
//                     entity, jobIndex, ref ECBuffer, agent, true);
//             }
//         }
//         
//         protected override JobHandle OnUpdate(JobHandle inputDeps)
//         {
//             Entities.ForEach()
//             var job = new ActionExecuteJob
//             {
//                 ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
//             };
//             var handle = job.Schedule(this, inputDeps);
//             ECBSystem.AddJobHandleForProducer(handle);
//             return handle;
//         }
//     }
// }