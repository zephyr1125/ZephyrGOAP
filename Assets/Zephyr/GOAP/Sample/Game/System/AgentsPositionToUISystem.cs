using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.UI;

namespace Zephyr.GOAP.Sample.Game.System
{
    public class AgentsPositionToUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithoutBurst()
                .WithAll<Agent, Translation>()
                .ForEach(
                (Entity entity, ref Translation translation) =>
                {
                    AgentInfoManager.Instance.UpdateAgentPosition(entity, translation);
                }).Run();
        }
    }
}