using System;
using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class ChopFirewoodAction : GoapAction
    {
        private bool _chopped = false;
        private ChoppingBlockComponent _targetChoppingBlock;

        private float _startTime = 0;
        public float workDuration = 2;

        public ChopFirewoodAction()
        {
            AddPrecondition("hasTool", true);
            AddPrecondition("hasFirewood", false);
            AddEffect("hasFirewood", true);
        }
        
        public override void Reset()
        {
            _chopped = false;
            _targetChoppingBlock = null;
            _startTime = 0;
        }

        public override bool IsDone()
        {
            return _chopped;
        }

        public override bool CheckProceduralPrecondition(GameObject agent)
        {
            var blocks = FindObjectsOfType<ChoppingBlockComponent>();
            ChoppingBlockComponent closest = null;
            float closestDist = 0;

            foreach (var block in blocks)
            {
                var distance = Vector3.Distance(transform.position, block.transform.position);
                if (closest == null)
                {
                    closest = block;
                    closestDist = distance;
                }else if (distance < closestDist)
                {
                    closest = block;
                    closestDist = distance;
                }
            }

            if (closest == null) return false;

            _targetChoppingBlock = closest;
            target = _targetChoppingBlock.gameObject;

            return closest != null;
        }

        public override bool Perform(GameObject agent)
        {
            if (Math.Abs(_startTime) < 0.1f) _startTime = Time.time;

            if (Time.time - _startTime > workDuration)
            {
                var backpack = agent.GetComponent<BackpackComponent>();
                backpack.numFirewood += 5;
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