using Unity.Entities;
using Unity.Transforms;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.UI;

namespace Zephyr.GOAP.Sample.Game.System
{
    public class AgentsPositionToUISystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<Agent, Translation>().ForEach(
                (Entity entity, ref Translation translation) =>
                {
                    AgentInfoManager.Instance.UpdateAgentPosition(entity, translation);
                });
        }
    }
}