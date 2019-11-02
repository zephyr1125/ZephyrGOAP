using System;
using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class PickUpLogsAction : GoapAction
    {
        private bool _hasLogs = false;
        private SupplyPileComponent _targetSupplyPile;

        public PickUpLogsAction()
        {
            AddPrecondition("hasLogs", false);
            AddEffect("hasLogs", true);
        }
        
        public override void Reset()
        {
            _hasLogs = false;
            _targetSupplyPile = null;
        }

        public override bool IsDone()
        {
            return _hasLogs;
        }

        public override bool CheckProceduralPrecondition(GameObject agent)
        {
            var supplyPiles = FindObjectsOfType<SupplyPileComponent>();
            SupplyPileComponent closest = null;
            float closestDistance = 0;

            foreach (var pile in supplyPiles)
            {
                var distance = Vector3.Distance(
                    transform.position, pile.transform.position);
                if (closest == null)
                {
                    closest = pile;
                    closestDistance = distance;
                }else if(distance<closestDistance)
                {
                    closest = pile;
                    closestDistance = distance;
                }
            }

            if (closest == null) return false;

            _targetSupplyPile = closest;
            target = _targetSupplyPile.gameObject;

            return closest != null;
        }

        public override bool Perform(GameObject agent)
        {
            if (_targetSupplyPile.numLogs > 0)
            {
                _targetSupplyPile.numLogs -= 1;
                _hasLogs = true;
                var backpack = agent.GetComponent<BackpackComponent>();
                backpack.numLogs += 1;

                return true;
            }

            return false;
        }

        public override bool RequiresInRange()
        {
            return true;
        }
    }
}