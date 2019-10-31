using System.Collections.Generic;
using UnityEngine;

namespace Classic.FSM
{
    public class FSM
    {
        private Stack<FSMState> _stateStack = new Stack<FSMState>();

        public delegate void FSMState(FSM fsm, GameObject gameObject);

        public void Update(GameObject gameObject)
        {
            if (_stateStack.Peek() != null)
            {
                _stateStack.Peek().Invoke(this, gameObject);
            }
        }

        public void PushState(FSMState state)
        {
            _stateStack.Push(state);
        }

        public void PopState()
        {
            _stateStack.Pop();
        }
    }
}