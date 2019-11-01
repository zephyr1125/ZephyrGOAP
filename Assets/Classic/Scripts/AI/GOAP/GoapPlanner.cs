using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Classic.AI.GOAP
{
    public class GoapPlanner
    {
        public Queue<GoapAction> Plan(GameObject agent, HashSet<GoapAction> availableActions,
            Dictionary<string, object> worldState, Dictionary<string, object> goal)
        {
            foreach (var action in availableActions)
            {
                action.DoReset();
            }
            
            var usableActions = new HashSet<GoapAction>();
            foreach (var action in availableActions)
            {
                if (action.CheckProceduralPrecondition(agent))
                {
                    usableActions.Add(action);
                }
            }

            // build up the tree and record the leaf nodes that provide a solution to the goal.
            var leaves = new List<Node>();
            
            var start = new Node(null, 0, worldState, null);
            var success = BuildGraph(start, leaves, usableActions, goal);

            if (!success)
            {
                Debug.Log("No Plan");
                return null;
            }

            Node cheapest = null;
            foreach (var leaf in leaves)
            {
                if (cheapest == null)
                {
                    cheapest = leaf;
                }else if (leaf.RunningCost < cheapest.RunningCost)
                {
                    cheapest = leaf;
                }
            }

            // get its node and work back through the parents
            List<GoapAction> result = new List<GoapAction>();
            Node n = cheapest;
            while (n != null)
            {
                if (n.Action != null)
                {
                    result.Insert(0, n.Action);
                }

                n = n.Parent;
            }
            // we now have this action list in correct order

            var queue = new Queue<GoapAction>();
            foreach (var action in result)
            {
                queue.Enqueue(action);
            }

            return queue;
        }

        private bool BuildGraph(Node parent, List<Node> leaves,
            HashSet<GoapAction> usableActions, Dictionary<string, object> goal)
        {
            var foundOne = false;

            foreach (var action in usableActions)
            {
                // if the parent state has the conditions for this action's preconditions, we can use it here
                if (InState(action.Preconditions, parent.State))
                {
                    // apply the action's effects to the parent state
                    var currentState = PopulateState(parent.State, action.Effects);
                    var node = new Node(parent, parent.RunningCost+action.cost, currentState, action);

                    if (InState(goal, currentState))
                    {
                        leaves.Add(node);
                        foundOne = true;
                    }
                    else
                    {
                        var subset = ActionSubset(usableActions, action);
                        var found = BuildGraph(node, leaves, subset, goal);
                        if (found) foundOne = true;
                    }
                }
            }

            return foundOne;
        }

        private HashSet<GoapAction> ActionSubset(HashSet<GoapAction> usableActions, GoapAction current)
        {
            var subset = new HashSet<GoapAction>();
            foreach (var action in usableActions)
            {
                if (action != current) subset.Add(action);
            }

            return subset;
        }

        private bool InState(Dictionary<string,object> test, Dictionary<string, object> state)
        {
            return test.All(state.Contains);
        }

        private Dictionary<string, object> PopulateState(
            Dictionary<string, object> original, Dictionary<string, object> effect)
        {
            var state = new Dictionary<string, object>();
            foreach (var o in original)
            {
                state.Add(o.Key, o.Value);
            }

            foreach (var e in effect)
            {
                if (state.ContainsKey(e.Key))
                {
                    state[e.Key] = e.Value;
                }
                else
                {
                    state.Add(e.Key, e.Value);
                }
            }

            return state;
        }

        private class Node
        {
            public readonly Node Parent;
            public readonly float RunningCost;
            public readonly Dictionary<string, object> State;
            public readonly GoapAction Action;

            public Node(Node parent, float runningCost, Dictionary<string, object> state, GoapAction action)
            {
                Parent = parent;
                RunningCost = runningCost;
                State = state;
                Action = action;
            }
        }
    }
}