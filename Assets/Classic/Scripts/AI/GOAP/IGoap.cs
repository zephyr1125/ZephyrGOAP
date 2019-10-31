using System.Collections.Generic;

namespace Classic.AI.GOAP
{
    public interface IGoap
    {
        Dictionary<string, object> GetWorldState();

        Dictionary<string, object> CreateGoalState();

        void PlanFailed(Dictionary<string, object> failedGoal);

        void PlanFound(Dictionary<string, object> goal, Queue<GoapAction> actions);

        void ActionsFinished();

        void PlanAborted(GoapAction abortCause);

        bool MoveAgent(GoapAction nextAction);
    }
}