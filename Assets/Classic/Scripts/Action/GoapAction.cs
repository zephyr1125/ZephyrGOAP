using System.Collections.Generic;
using UnityEngine;

namespace Classic.Action
{
    public abstract class GoapAction : MonoBehaviour
    {
        private HashSet<KeyValuePair<string, object>> _preconditions;
        private HashSet<KeyValuePair<string, object>> effects;
    }
}
