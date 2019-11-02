using System;
using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class ChopTreeAction : GoapAction
    {
        private bool _chopped = false;
        private TreeComponent _targetChoppingTree;

        private float _startTime = 0;
        public float workDuration = 2;

        public ChopTreeAction()
        {
            AddPrecondition("hasTool", true);
            AddPrecondition("hasLogs", false);
            AddEffect("hasLogs", true);
        }
        
        public override void Reset()
        {
            _chopped = false;
            _targetChoppingTree = null;
            _startTime = 0;
        }

        public override bool IsDone()
        {
            return _chopped;
        }

        public override bool CheckProceduralPrecondition(GameObject agent)
        {
            var trees = FindObjectsOfType<TreeComponent>();
            TreeComponent closest = null;
            float closestDist = 0;

            foreach (var tree in trees)
            {
                var distance = Vector3.Distance(
                    transform.position, tree.transform.position);
                if (closest == null)
                {
                    closest = tree;
                    closestDist = distance;
                }else if (distance < closestDist)
                {
                    closest = tree;
                    closestDist = distance;
                }
            }

            if (closest == null) return false;

            _targetChoppingTree = closest;
            target = _targetChoppingTree.gameObject;

            return closest != null;
        }

        public override bool Perform(GameObject agent)
        {
            if (Math.Abs(_startTime) < 0.1f) _startTime = Time.time;

            if (Time.time - _startTime > workDuration)
            {
                var backpack = agent.GetComponent<BackpackComponent>();
                backpack.numLogs += 1;
                _chopped = true;

                var tool = backpack.tool.GetComponent<ToolComponent>();
                tool.Use(0.34f);
                if (tool.IsDestroyed())
                {
                    Destroy(backpack.tool);
                    backpack.tool = null;
                }
            }

            return true;
        }

        public override bool RequiresInRange()
        {
            return true;
        }
    }
}