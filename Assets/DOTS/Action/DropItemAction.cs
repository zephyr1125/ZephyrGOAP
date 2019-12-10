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
            foreach (var targetState in targetStates)
            {
                //只针对非自身目标的原料请求的goal state
                if (targetState.Target == stackData.AgentEntity) continue;
                if (targetState.Trait != typeof(ItemContainerTrait)) continue;

                return targetState;
            }

            return default;
        }

        public void GetPreconditions(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                Value = targetState.Value,
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
                Value = targetState.Value,
                IsPositive = true,
            });
        }

        public Entity GetNavigatingSubject(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            return targetState.Target;
        }
    }
}