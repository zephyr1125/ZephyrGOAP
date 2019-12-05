using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System
{
    public class NavigatingEndSystem : JobComponentSystem
    {
        
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
}