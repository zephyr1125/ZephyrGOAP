using System.Collections.Generic;
using UnityEngine;

namespace Classic.Action
{
    public abstract class GoapAction : MonoBehaviour
    {
        private Dictionary<string, object> _preconditions;
        private Dictionary<string, object> _effects;

        private bool _inRange = false;

        public float cost = 1f;
        
        public GameObject target;

        public GoapAction()
        {
            _preconditions = new Dictionary<string, object>();
            _effects = new Dictionary<string, object>();
        }

        public void DoReset()
        {
            _inRange = false;
            target = null;
            Reset();
        }

        public abstract void Reset();

        public abstract bool IsDone();

        public abstract bool CheckProceduralPrecondition(GameObject agent);

        public abstract bool Perform(GameObject agent);

        public abstract bool RequiresInRange();

        public bool IsInRange()
        {
            return _inRange;
        }

        public void SetInRange(bool inRange)
        {
            _inRange = inRange;
        }

        public void AddPrecondition(string key, object value)
        {
            _preconditions.Add(key, value);
        }

        public void RemovePrecondition(string key)
        {
            _preconditions.Remove(key);
        }
        
        public void AddEffect(string key, object value)
        {
            _effects.Add(key, value);
        }

        public void RemoveEffect(string key)
        {
            _effects.Remove(key);
        }

        public Dictionary<string, object> Preconditions => _preconditions;

        public Dictionary<string, object> Effects => _effects;
    }
}
