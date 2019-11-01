using System;
using System.Collections.Generic;
using UnityEngine;

namespace Classic.AI.GOAP
{
    public class GoapAgent : MonoBehaviour
    {
        private FSM.FSM _stateMachine;

        private FSM.FSM.FSMState _idleState;
        private FSM.FSM.FSMState _moveToState;
        private FSM.FSM.FSMState _performActionState;

        private HashSet<GoapAction> _availableActions;
        private Queue<GoapAction> _currentActions;

        private IGoap _dataProvider;

        private GoapPlanner _planner;

        private void Start()
        {
            _stateMachine = new FSM.FSM();
            _availableActions = new HashSet<GoapAction>();
            _currentActions = new Queue<GoapAction>();
            _planner = new GoapPlanner();

            FindDataProvider();
            
            CreateIdleState();
            CreateMoveToState();
            CreatePerformActionState();
            
            _stateMachine.PushState(_idleState);

            LoadActions();
        }

        private void Update()
        {
            _stateMachine.Update(gameObject);
        }

        public void AddAction(GoapAction action)
        {
            _availableActions.Add(action);
        }

        public T GetAction<T>() where T : GoapAction
        {
            foreach (var action in _availableActions)
            {
                if (action is T)
                {
                    return action as T;
                }
            }
            return null;
        }

        public void RemoveAction(GoapAction action)
        {
            _availableActions.Remove(action);
        }

        private bool HasActionPlan()
        {
            return _currentActions.Count > 0;
        }

        private void CreateIdleState()
        {
            _idleState = (fsm, go) =>
            {
                var worldState = _dataProvider.GetWorldState();
                var goal = _dataProvider.CreateGoalState();

                var plan = _planner.Plan(gameObject, _availableActions, worldState, goal);
                if (plan != null)
                {
                    _currentActions = plan;
                    _dataProvider.PlanFound(goal, plan);

                    fsm.PopState();
                    fsm.PushState(_performActionState);
                }
                else
                {
                    Debug.Log("Fail Plan of " + Utils.PrettyPrint(goal));
                    _dataProvider.PlanFailed(goal);

                    fsm.PopState();
                    fsm.PushState(_idleState);
                }
            };
        }

        private void CreateMoveToState()
        {
            _moveToState = (fsm, go) =>
            {
                var action = _currentActions.Peek();
                
                if (action.RequiresInRange() && action.target == null)
                {
                    Debug.Log("Action need a target:" + action);
                    fsm.PopState();//move
                    fsm.PopState();//perform
                    fsm.PushState(_idleState);
                }

                if (_dataProvider.MoveAgent(action))
                {
                    fsm.PopState();
                }
            };
        }

        private void CreatePerformActionState()
        {
            _performActionState = (fsm, go) =>
            {
                if (!HasActionPlan())
                {
                    Debug.Log("Actions Done");
                    fsm.PopState();
                    fsm.PushState(_idleState);
                    _dataProvider.ActionsFinished();
                    return;
                }

                var action = _currentActions.Peek();
                if (action.IsDone())
                {
                    _currentActions.Dequeue();
                }

                if (HasActionPlan())
                {
                    action = _currentActions.Peek();
                    bool inRange = !action.RequiresInRange() || action.IsInRange();

                    if (inRange)
                    {
                        var success = action.Perform(go);
                        if (success) return;
                        
                        fsm.PopState();
                        fsm.PushState(_idleState);
                        _dataProvider.PlanAborted(action);
                    }
                    else
                    {
                        fsm.PushState(_moveToState);
                    }
                }
                else
                {
                    fsm.PopState();
                    fsm.PushState(_idleState);
                    _dataProvider.ActionsFinished();
                }
            };
        }

        private void FindDataProvider()
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component is IGoap)
                {
                    _dataProvider = (IGoap)component;
                    return;
                }
            }
        }

        private void LoadActions()
        {
            var actions = gameObject.GetComponents<GoapAction>();
            foreach (var action in actions)
            {
                _availableActions.Add(action);
            }
            Debug.Log("Found actions: "+Utils.PrettyPrint(actions));
        }
    }
}