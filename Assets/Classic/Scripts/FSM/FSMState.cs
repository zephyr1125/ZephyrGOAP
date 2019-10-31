using UnityEngine;

namespace Classic.FSM
{
    public interface FSMState
    {
        void Update(FSM fsm, GameObject gameObject);
    }
}