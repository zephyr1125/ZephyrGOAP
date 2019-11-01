using System.Collections.Generic;

namespace Classic.Game.Labor
{
    public class Miner : Labor
    {
        public override Dictionary<string, object> CreateGoalState()
        {
            return new Dictionary<string, object>{{"collectOre", true}};
        }
    }
}