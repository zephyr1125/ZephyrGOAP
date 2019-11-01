using System;
using System.Collections.Generic;
using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Labor
{
    public abstract class Labor : MonoBehaviour, IGoap
    {
        public BackpackComponent backpack;
        public float moveSpeed = 1;

        private void Start()
        {
            if (backpack == null) backpack = gameObject.AddComponent<BackpackComponent>();

            if (backpack.tool == null)
            {
                var prefab = Resources.Load<GameObject>(backpack.toolType);
                var tool = Instantiate(prefab, transform);
                backpack.tool = tool;
            }
        }

        public Dictionary<string, object> GetWorldState()
        {
            return new Dictionary<string, object>
            {
                {"hasOre", backpack.numOre > 0},
                {"hasLogs", backpack.numLogs > 0},
                {"hasFirewood", backpack.numFirewood > 0},
                {"hasTool", backpack.tool != null}
            };
        }

        public abstract Dictionary<string, object> CreateGoalState();

        public void PlanFailed(Dictionary<string, object> failedGoal)
        {
            // Not handling this here since we are making sure our goals will always succeed.
            // But normally you want to make sure the world state has changed before running
            // the same goal again, or else it will just fail.
        }

        public void PlanFound(Dictionary<string, object> goal, Queue<GoapAction> actions)
        {
            Debug.Log("Found plan for "+Utils.PrettyPrint(goal)
                                       + " : "+Utils.PrettyPrint(actions));
        }

        public void ActionsFinished()
        {
            Debug.Log("Actions finished");
        }

        public void PlanAborted(GoapAction abortCause)
        {
            // An action bailed out of the plan. State has been reset to plan again.
            // Take note of what happened and make sure if you run the same goal again
            // that it can succeed.
            Debug.Log("Plan aborted "+Utils.PrettyPrint(abortCause));
        }

        public bool MoveAgent(GoapAction nextAction)
        {
            var targetPos = nextAction.target.transform.position;
            var step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

            if (transform.position.Equals(targetPos))
            {
                nextAction.SetInRange(true);
                return true;
            }

            return false;
        }
    }
}