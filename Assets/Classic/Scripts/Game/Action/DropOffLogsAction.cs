using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class DropOffLogsAction : GoapAction
    {
        private bool _droppedOffLog = false;
        private SupplyPileComponent _targetSupplyPile;

        public DropOffLogsAction()
        {
            AddPrecondition("hasLogs", true);
            AddEffect("hasLogs", false);
            AddEffect("collectLogs", true);
        }
        
        public override void Reset()
        {
            _droppedOffLog = false;
            _targetSupplyPile = null;
        }

        public override bool IsDone()
        {
            return _droppedOffLog;
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
            var backpack = agent.GetComponent<BackpackComponent>();
            _targetSupplyPile.numLogs += backpack.numLogs;
            _droppedOffLog = true;
            backpack.numLogs = 0;

            return true;
        }

        public override bool RequiresInRange()
        {
            return true;
        }
    }
}