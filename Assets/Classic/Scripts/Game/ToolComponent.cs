using System;
using UnityEngine;

namespace Classic.Game
{
    public class ToolComponent : MonoBehaviour
    {
        public float strength;

        private void Start()
        {
            strength = 1;
        }

        public void Use(float damage)
        {
            strength -= damage;
        }

        public bool IsDestroyed()
        {
            return strength <= 0f;
        }
    }
}