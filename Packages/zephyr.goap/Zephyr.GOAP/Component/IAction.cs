using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Component
{
    public interface IAction
    {
        NativeString32 GetName();
        
        State GetTargetRequire(ref StateGroup targetRequires, Entity agentEntity, ref StackData stackData);

        StateGroup GetSettings(ref State targetState, Entity agentEntity, ref StackData stackData, Allocator allocator);

        void GetPreconditions([ReadOnly] ref State targetState, Entity agentEntity, ref State setting,
            [ReadOnly] ref StackData stackData, ref StateGroup preconditions);

        void GetEffects([ReadOnly] ref State targetState, ref State setting,
            [ReadOnly] ref StackData stackData, ref StateGroup effects);
        
        float GetReward([ReadOnly] ref State targetState, ref State setting,
            [ReadOnly] ref StackData stackData);
        
        float GetExecuteTime([ReadOnly] ref State targetState, ref State setting,
            [ReadOnly] ref StackData stackData);

        void GetNavigatingSubjectInfo(ref State targetState, ref State setting,
            [ReadOnly] ref StackData stackData, ref StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId);
    }
}