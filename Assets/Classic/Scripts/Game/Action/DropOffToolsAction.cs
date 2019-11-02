using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class DropOffToolsAction : GoapAction
    {
        private bool _droppedOffTools = false;
        private SupplyPileComponent _targetSupplyPile;

        public DropOffToolsAction()
        {
            AddPrecondition("hasNewTools", true);
            AddEffect("hasNewTools", false);
            AddEffect("collectTools", true);
        }
        
        public override void Reset()
        {
            _droppedOffTools = false;
            _targetSupplyPile = null;
        }

        public override bool IsDone()
        {
            return _droppedOffTools;
        }

        public override bool CheckProceduralPrecondition(GameObject agent)
        {
            var supplyPiles = FindObjectsOfType<SupplyPileComponent>();
            SupplyPileComponent closest = null;
            float closestDist = 0;

            foreach (var pile in supplyPiles)
            {
                var distance = Vector3.Distance(
                    transform.position, pile.transform.position);
                if (closest == null)
                {
                    closest = pile;
                    closestDist = distance;
                }else if (distance < closestDist)
                {
                    closest = pile;
                    closestDist = distance;
                }
            }

            if (closest == null) return false;

            _targetSupplyPile = closest;
            target = _targetSupplyPile.gameObject;

            return closest != null;
        }

        public override bool Perform(GameObject agent)
        {
            _targetSupplyPile.numTools += 2;
            _droppedOffTools = true;

            return true;
        }

        public override bool RequiresInRange()
        {
            return true;
        }
    }
}