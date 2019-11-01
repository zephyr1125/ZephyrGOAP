using System.Collections.Generic;

namespace Classic.Game.Labor
{
    public class WoodCutter : Labor
    {
        public override Dictionary<string, object> CreateGoalState()
        {
            return new Dictionary<string, object>{{"collectFirewood", true}};
        }
    }
}