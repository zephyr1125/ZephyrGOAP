using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Entities;
using NotImplementedException = System.NotImplementedException;

namespace DOTS.Action
{
    public struct DropItemAction : IComponentData, IAction
    {
        public NativeString64 GetName()
        {
            return new NativeString64(nameof(DropItemAction));
        }

        public State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData)
        {
            var stateFilter = new State
            {
                SubjectType = StateSubjectType.Target,
                Trait = typeof(ItemContainerTrait),
                IsPositive = true
            };
            return targetStates.GetBelongingState(stateFilter);
        }

        public void GetPreconditions(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = targetState.ValueString,
                IsPositive = true
            });
        }

        public void GetEffects(ref State targetState, ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(new State
            {
                SubjectType = StateSubjectType.Target,
                Target = targetState.Target,
                Trait = typeof(ItemContainerTrait),
                ValueString = targetState.ValueString,
                IsPositive = true,
            });
        }

        public Entity GetNavigatingSubject(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            return targetState.Target;
        }
    }
}