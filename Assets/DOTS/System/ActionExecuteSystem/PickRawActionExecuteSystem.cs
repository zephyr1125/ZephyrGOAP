using DOTS.Component;
using DOTS.Component.Actions;
using DOTS.Struct;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.ActionJob
{
    public class PickRawActionExecuteSystem : JobComponentSystem
    {
        public struct PickRawActionExecuteJob : IJobForEach_BBBC<Action, Node, State, Agent>
        {
            public void Execute(DynamicBuffer<Action> actions, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent)
            {
                if (agent.GoapState != GoapState.ReadyToExecute) return;
                
                //no suitable action, no execute
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}