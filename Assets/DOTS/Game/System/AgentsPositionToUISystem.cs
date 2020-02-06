using DOTS.Component;
using DOTS.Game.UI;
using Unity.Entities;
using Unity.Transforms;

namespace DOTS.Game.System
{
    public class AgentsPositionToUISystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<Agent, Translation>().ForEach(
                (Entity entity, ref Translation translation) =>
                {
                    AgentTalkManager.Instance.UpdateAgentPosition(entity, translation);
                });
        }
    }
}