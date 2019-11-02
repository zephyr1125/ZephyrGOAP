using System;
using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class ForgeToolAction : GoapAction
    {
        private bool _forged = false;
        private ForgeComponent _targetForge;

        private float _startTime = 0;
        private float forgeDuration = 2;

        public ForgeToolAction()
        {
            AddPrecondition("hasOre", true);
            AddEffect("hasNewTools", true);
        }
        
        public override void Reset()
        {
            _forged = false;
            _targetForge = null;
            _startTime = 0;
        }

        public override bool IsDone()
        {
            return _forged;
        }

        public override bool CheckProceduralPrecondition(GameObject agent)
        {
            var forges = FindObjectsOfType<ForgeComponent>();
            ForgeComponent closest = null;
            float closestDist = 0;

            foreach (var forge in forges)
            {
                var distance = Vector3.Distance(
                    transform.position, forge.transform.position);
                if (closest == null)
                {
                    closest = forge;
                    closestDist = distance;
                }else if (distance < closestDist)
                {
                    closest = forge;
                    closestDist = distance;
                }
            }

            if (closest == null) return false;

            _targetForge = closest;
            target = _targetForge.gameObject;

            return closest != null;
        }

        public override bool Perform(GameObject agent)
        {
            if (Math.Abs(_startTime) < 0.1f)
            {
                _startTime = Time.time;
            }

            if (Time.time - _startTime > forgeDuration)
            {
                var backpack = agent.GetComponent<BackpackComponent>();
                backpack.numOre = 0;
                _forged = true;
            }

            return true;
        }

        public override bool RequiresInRange()
        {
            return true;
        }
    }
}