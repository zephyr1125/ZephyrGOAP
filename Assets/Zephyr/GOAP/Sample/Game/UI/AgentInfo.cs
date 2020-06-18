using TMPro;
using UnityEngine;

namespace Zephyr.GOAP.Sample.Game.UI
{
    public class AgentInfo : MonoBehaviour
    {
        public TMP_Text ActionText;
        public TMP_Text StaminaText;
        
        private Camera _camera;
        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
            _camera = Camera.main;
        }

        public void SetPosition(Vector3 agentPosition)
        {
            _transform.position = _camera.WorldToScreenPoint(agentPosition) + new Vector3(0, 84, 0);
        }

        public void SetActionText(string text)
        {
            ActionText.text = text;
        }
        
        public void SetStaminaText(float stamina)
        {
            StaminaText.text = $"{stamina:F1}";
        }
    }
}