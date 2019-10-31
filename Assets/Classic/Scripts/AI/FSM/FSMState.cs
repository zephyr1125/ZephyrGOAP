using UnityEngine;

namespace Classic.AI.FSM
{
    public interface FSMState
    {
        void Update(FSM fsm, GameObject gameObject);
    }
}