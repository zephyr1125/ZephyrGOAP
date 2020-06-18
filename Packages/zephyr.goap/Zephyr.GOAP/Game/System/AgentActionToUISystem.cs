// using Unity.Entities;
// using Zephyr.GOAP.Component;
// using Zephyr.GOAP.Game.ComponentData;
// using Zephyr.GOAP.Game.UI;
// using Zephyr.GOAP.Struct;
//
// namespace Zephyr.GOAP.Game.System
// {
//     public class AgentActionToUISystem : ComponentSystem
//     {
//         protected override void OnUpdate()
//         {
//             Entities.ForEach(
//                 (Entity entity, DynamicBuffer<Node> nodes, ref Agent agent, ref Stamina stamina) =>
//                 {
//                     if (agent.ExecutingNodeId >= nodes.Length) return;
//                     
//                     var currentNode = nodes[agent.ExecutingNodeId];
//                     AgentInfoManager.Instance.SetAgentText(entity, 
//                         currentNode.Name.ToString().Replace("Action", ""),
//                         stamina.Value);
//             });
//         }
//     }
// }