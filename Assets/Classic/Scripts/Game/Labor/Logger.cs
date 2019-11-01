using System.Collections.Generic;

namespace Classic.Game.Labor
{
    public class Logger : Labor
    {
        public override Dictionary<string, object> CreateGoalState()
        {
            return new Dictionary<string, object>{{"collectLogs", true}};
        }
    }
}