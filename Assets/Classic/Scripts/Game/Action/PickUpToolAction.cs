using System;
using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class PickUpToolAction : GoapAction
    {
        private bool _hasTool = false;
        private SupplyPileComponent _targetSupplyPile;

        public PickUpToolAction()
        {
            AddPrecondition("hasTool", false);
            AddEffect("hasTool", true);
        }
        
        public override void Reset()
        {
            _hasTool = false;
            _targetSupplyPile = null;
        }

        public override bool IsDone()
        {
            return _hasTool;
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
            if (_targetSupplyPile.numTools > 0)
            {
                _targetSupplyPile.numTools -= 1;
                _hasTool = true;
                
                var backpack = agent.GetComponent<BackpackComponent>();
                var prefab = Resources.Load<GameObject>(backpack.toolType);
                var tool = Instantiate(prefab, transform);
                backpack.tool = tool;

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