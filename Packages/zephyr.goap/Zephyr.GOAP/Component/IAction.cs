using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Component
{
    public interface IAction
    {
        bool CheckTargetRequire(State targetRequire, Entity agentEntity, [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates);

        StateGroup GetSettings(State targetRequire, Entity agentEntity, [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, Allocator allocator);

        void GetPreconditions([ReadOnly] State targetRequire, Entity agentEntity, State setting,
            [ReadOnly] StackData stackData, [ReadOnly]StateGroup currentStates, StateGroup preconditions);

        void GetEffects([ReadOnly] State targetRequire, State setting,
            [ReadOnly] StackData stackData, StateGroup effects);
        
        float GetReward([ReadOnly] State targetRequire, State setting,
            [ReadOnly] StackData stackData);
        
        float GetExecuteTime([ReadOnly] State setting);

        void GetNavigatingSubjectInfo(State targetRequire, State setting,
            [ReadOnly] StackData stackData, StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId);
    }
}