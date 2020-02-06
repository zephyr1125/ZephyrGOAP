using DOTS.Component;
using DOTS.Game.UI;
using DOTS.Struct;
using Unity.Entities;

namespace DOTS.Game.System
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