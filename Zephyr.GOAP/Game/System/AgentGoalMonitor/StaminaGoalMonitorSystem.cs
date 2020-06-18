// using Unity.Entities;
// using Zephyr.GOAP.Component;
// using Zephyr.GOAP.Component.Trait;
// using Zephyr.GOAP.Game.ComponentData;
// using Zephyr.GOAP.Struct;
// using Zephyr.GOAP.System.GoalManage;
//
// namespace Zephyr.GOAP.Game.System.AgentGoalMonitor
// {
//     public class StaminaGoalMonitorSystem : AgentGoalMonitorComponentSystem
//     {
//         private const float StaminaThreshold = 0.5f;
//
//         protected override float GetTimeInterval()
//         {
//             return Utils.GoalMonitorSystemInterval;
//         }
//
//         protected override void OnMonitorUpdate()
//         {
//             Entities.WithAll<Agent>().ForEach(
//                 (Entity entity, ref Stamina stamina)=>
//             {
//                 if (stamina.Value >= StaminaThreshold) return;
//                 
//                 AddGoal(entity, new State
//                 {
//                     Target = entity,
//                     Trait = typeof(StaminaTrait),
//                 }, Time.ElapsedTime);
//             });
//         }
//     }
// }