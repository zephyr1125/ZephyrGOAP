using DOTS.Action;
using DOTS.Component;
using DOTS.Struct;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.ActionJob
{
    public class PickRawActionExecuteSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(PickRawAction))]
        public struct PickRawActionExecuteJob : IJobForEach_BBC<Node, State, Agent>
        {
            public void Execute(DynamicBuffer<Node> nodes,
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