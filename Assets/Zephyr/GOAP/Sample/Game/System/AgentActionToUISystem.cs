using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.UI;

namespace Zephyr.GOAP.Sample.Game.System
{
    public class AgentActionToUISystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var nodes = GetComponentDataFromEntity<Node>();
            Entities
                .WithReadOnly(nodes)
                .WithoutBurst()
                .ForEach(
                (Entity entity, in Stamina stamina, in Acting acting) =>
                {
                    var currentNode = nodes[acting.NodeEntity];
                    AgentInfoManager.Instance.SetAgentText(entity, 
                        currentNode.Name.ToString().Replace("Action", ""),
                        stamina.Value);
            }).Run();
        }
    }
}