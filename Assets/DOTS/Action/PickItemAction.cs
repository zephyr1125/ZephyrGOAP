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
            foreach (var targetState in targetStates)
            {
                //只针对要求自身具有原料请求的goal state
                if (targetState.Target != stackData.AgentEntity) continue;
                if (targetState.Trait != typeof(ItemContainerTrait)) continue;

                return targetState;
            }

            return default;
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
                ValueString = targetState.ValueString,
                IsPositive = true,
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
                SubjectType = StateSubjectType.Self,
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = targetState.ValueString,
                IsPositive = true
            });
        }
        
        public Entity GetNavigatingSubject(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            return preconditions[0].Target;
        }
    }
}