using System;
using Classic.AI.GOAP;
using UnityEngine;

namespace Classic.Game.Action
{
    public class MineOreAction : GoapAction
    {
        public float miningDuration = 2;

        private bool _mined = false;
        private IronRockComponent _targetRock;
        private float _startTime = 0;

        public MineOreAction()
        {
            AddPrecondition("hasTool", true);
            AddPrecondition("hasOre", false);
            
            AddEffect("hasOre", true);
        }
        
        public override void Reset()
        {
            _mined = false;
            _startTime = 0;
            _targetRock = null;
        }

        public override bool IsDone()
        {
            return _mined;
        }

        public override bool CheckProceduralPrecondition(GameObject agent)
        {
            var rocks = FindObjectsOfType<IronRockComponent>();
            IronRockComponent closest = null;
            float closestDistance = 0;

            foreach (var rock in rocks)
            {
                var distance = Vector3.Distance(
                    transform.position, rock.transform.position);
                if (closest == null)
                {
                    closest = rock;
                    closestDistance = distance;
                }else if(distance<closestDistance)
                {
                    closest = rock;
                    closestDistance = distance;
                }
            }

            _targetRock = closest;
            if(_targetRock!=null)target = _targetRock.gameObject;

            return closest != null;
        }

        public override bool Perform(GameObject agent)
        {
            if (Math.Abs(_startTime) < 0.1f) _startTime = Time.time;

            if (Time.time - _startTime > miningDuration)
            {
                var backpack = agent.GetComponent<BackpackComponent>();
                backpack.numOre += 2;
                _mined = true;

                var tool = backpack.tool.GetComponent<ToolComponent>();
                tool.Use(0.5f);
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