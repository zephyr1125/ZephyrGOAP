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
            //针对“目标获得物品”的state
            var stateFilter = new State
            {
                Trait = typeof(ItemContainerTrait),
            };
            var agent = stackData.AgentEntity;
            //额外：target不能为自身
            return targetStates.GetState(state => state.Target != agent && state.BelongTo(stateFilter));
        }

        public void GetPreconditions(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = targetState.ValueString,
            });
        }

        public void GetEffects(ref State targetState, ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(new State
            {
                Target = targetState.Target,
                Trait = typeof(ItemContainerTrait),
                ValueString = targetState.ValueString,
            });
        }

        public Entity GetNavigatingSubject(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            return targetState.Target;
        }
    }
}