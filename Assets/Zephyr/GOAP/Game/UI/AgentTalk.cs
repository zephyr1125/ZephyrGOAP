using TMPro;
using UnityEngine;

namespace Zephyr.GOAP.Game.UI
{
    public class AgentTalk : MonoBehaviour
    {
        public TMP_Text Text;
        
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

        public void SetText(string text)
        {
            Text.text = text;
        }
    }
}