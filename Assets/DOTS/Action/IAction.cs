using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Action
{
    public interface IAction
    {
        NativeString64 GetName();
        
        State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData);

        void GetPreconditions([ReadOnly] ref State targetState,
            [ReadOnly] ref StackData stackData, ref StateGroup preconditions);

        void GetEffects([ReadOnly] ref State targetState,
            [ReadOnly] ref StackData stackData, ref StateGroup effects);

        Entity GetNavigatingSubject(ref State targetState, [ReadOnly] ref StackData stackData,
            ref StateGroup preconditions);
    }
}