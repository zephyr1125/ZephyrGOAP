using System.Collections.Generic;

namespace Classic.Game.Labor
{
    public class Blacksmith : Labor
    {
        public override Dictionary<string, object> CreateGoalState()
        {
            return new Dictionary<string, object> {{"collectTools", true}};
        }
    }
}