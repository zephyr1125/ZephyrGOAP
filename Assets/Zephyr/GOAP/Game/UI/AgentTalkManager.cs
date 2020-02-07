using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Zephyr.GOAP.Game.UI
{
    public class AgentTalkManager : MonoBehaviour
    {
        private static AgentTalkManager _instance;

        public static AgentTalkManager Instance => _instance;

        public GameObject AgentTalkGameObject;

        private Dictionary<Entity, AgentTalk> _agentTalks;

        private void Awake()
        {
            _instance = this;
            _agentTalks = new Dictionary<Entity, AgentTalk>();
        }

        private AgentTalk AddAgentTalk(Entity agentEntity)
        {
            var go = Instantiate(AgentTalkGameObject, transform);
            go.SetActive(true);
            var agentTalk = go.GetComponent<AgentTalk>();
            _agentTalks.Add(agentEntity, agentTalk);
            return agentTalk;
        }

        public void UpdateAgentPosition(Entity agentEntity, Translation translation)
        {
            var talk = 
                !_agentTalks.ContainsKey(agentEntity) ? AddAgentTalk(agentEntity) : _agentTalks[agentEntity];
            talk.SetPosition(translation.Value);
        }

        public void SetAgentText(Entity agentEntity, string text)
        {
            var talk = 
                !_agentTalks.ContainsKey(agentEntity) ? AddAgentTalk(agentEntity) : _agentTalks[agentEntity];
            talk.SetText(text);
        }
    }
}