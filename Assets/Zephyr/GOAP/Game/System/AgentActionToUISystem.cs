using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Game.UI;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Game.System
{
    public class AgentActionToUISystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<Agent, Node>().ForEach(
                (Entity entity, ref Agent agent, DynamicBuffer<Node> nodes) =>
                {
                    if (agent.ExecutingNodeId >= nodes.Length) return;
                    
                    var currentNode = nodes[agent.ExecutingNodeId];
                    AgentTalkManager.Instance.SetAgentText(entity, 
                        currentNode.Name.ToString().Replace("Action", ""));
            });
        }
    }
}