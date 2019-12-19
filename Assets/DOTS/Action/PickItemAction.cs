using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.Action
{
    public struct PickItemAction : IComponentData, IAction
    {
        public NativeString64 GetName()
        {
            return new NativeString64(nameof(PickItemAction));
        }
        
        public State GetTargetGoalState([ReadOnly]ref StateGroup targetStates,
            [ReadOnly]ref StackData stackData)
        {
            //针对“自身获得物品”的state
            var stateFilter = new State
            {
                Trait = typeof(ItemContainerTrait),
            };
            var agent = stackData.AgentEntity;
            return targetStates.GetState(state => state.Target == agent && state.BelongTo(stateFilter));
        }
        
        /// <summary>
        /// 条件：世界里要有对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        public void GetPreconditions([ReadOnly]ref State targetState,
            [ReadOnly]ref StackData stackData, ref StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                Trait = typeof(ItemContainerTrait),
                ValueTrait = targetState.ValueTrait,
                ValueString = targetState.ValueString,
            });
        }

        /// <summary>
        /// 效果：自身获得对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        public void GetEffects([ReadOnly]ref State targetState,
            [ReadOnly]ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(new State
            {
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = targetState.ValueTrait,
                ValueString = targetState.ValueString,
            });
        }
        
        public Entity GetNavigatingSubject(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            return preconditions[0].Target;
        }
    }
}