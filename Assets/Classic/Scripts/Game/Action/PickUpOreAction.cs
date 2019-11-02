using System;
using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class PickUpOreAction : GoapAction
    {
        private bool _hasOre = false;
        private SupplyPileComponent _targetSupplyPile;

        public PickUpOreAction()
        {
            AddPrecondition("hasOre", false);
            AddEffect("hasOre", true);
        }
        
        public override void Reset()
        {
            _hasOre = false;
            _targetSupplyPile = null;
        }

        public override bool IsDone()
        {
            return _hasOre;
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
            if (_targetSupplyPile.numOre >= 3)
            {
                _targetSupplyPile.numLogs -= 3;
                _hasOre = true;
                var backpack = agent.GetComponent<BackpackComponent>();
                backpack.numOre += 3;

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