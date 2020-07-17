using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Component
{
    public interface IAction
    {
        NativeString32 GetName();
        
        bool CheckTargetRequire(State targetRequire, Entity agentEntity, [ReadOnly]StackData stackData);

        StateGroup GetSettings(State targetRequire, Entity agentEntity, [ReadOnly]StackData stackData, Allocator allocator);

        void GetPreconditions([ReadOnly] State targetRequire, Entity agentEntity, State setting,
            [ReadOnly] StackData stackData, StateGroup preconditions);

        void GetEffects([ReadOnly] State targetRequire, State setting,
            [ReadOnly] StackData stackData, StateGroup effects);
        
        float GetReward([ReadOnly] State targetRequire, State setting,
            [ReadOnly] StackData stackData);
        
        float GetExecuteTime([ReadOnly] State targetRequire, State setting,
            [ReadOnly] StackData stackData);

        void GetNavigatingSubjectInfo(State targetRequire, State setting,
            [ReadOnly] StackData stackData, StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId);
    }
}