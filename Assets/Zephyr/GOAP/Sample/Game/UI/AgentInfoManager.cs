using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Zephyr.GOAP.Sample.Game.UI
{
    public class AgentInfoManager : MonoBehaviour
    {
        private static AgentInfoManager _instance;

        public static AgentInfoManager Instance => _instance;

        public GameObject AgentInfoGameObject;

        private Dictionary<Entity, AgentInfo> _agentInfos;

        private void Awake()
        {
            _instance = this;
            _agentInfos = new Dictionary<Entity, AgentInfo>();
        }

        private AgentInfo AddAgentTalk(Entity agentEntity)
        {
            var go = Instantiate(AgentInfoGameObject, transform);
            go.SetActive(true);
            var agentInfo = go.GetComponent<AgentInfo>();
            _agentInfos.Add(agentEntity, agentInfo);
            return agentInfo;
        }

        public void UpdateAgentPosition(Entity agentEntity, Translation translation)
        {
            var info = 
                !_agentInfos.ContainsKey(agentEntity) ? AddAgentTalk(agentEntity) : _agentInfos[agentEntity];
            info.SetPosition(translation.Value);
        }

        public void SetAgentText(Entity agentEntity, string actionText, float stamina)
        {
            var info = 
                !_agentInfos.ContainsKey(agentEntity) ? AddAgentTalk(agentEntity) : _agentInfos[agentEntity];
            info.SetActionText(actionText);
            info.SetStaminaText(stamina);
        }
    }
}