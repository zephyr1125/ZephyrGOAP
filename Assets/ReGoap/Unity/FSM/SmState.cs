using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace ReGoap.Unity.FSM
{
    public class SmState : MonoBehaviour, ISmState
    {
        public List<ISmTransition> Transitions { get; set; }
        public int priority;

        private void Awake()
        {
            Transitions = new List<ISmTransition>();
        }

        public virtual void Enter()
        {
            
        }

        public virtual void Exit()
        {
            
        }

        public virtual void Init(StateMachine stateMachine)
        {
            
        }

        public virtual bool IsActive()
        {
            return enabled;
        }

        public virtual int GetPriority()
        {
            return priority;
        }
    }
}