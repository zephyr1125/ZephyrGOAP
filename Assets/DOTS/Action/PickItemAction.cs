using System.Linq;
using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Action
{
    /// <summary>
    /// 每个setting代表一个适合的物品来源
    /// </summary>
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

        public StateGroup GetSettings(ref State targetState, ref StackData stackData, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            
            if (!targetState.ValueString.Equals(new NativeString64()))
            {
                //如果指定了物品名，那么只有一种setting，也就是targetState本身
                settings.Add(targetState);
            }else if (targetState.ValueString.Equals(new NativeString64()) &&
                      targetState.ValueTrait != null)
            {
                //如果targetState是类别范围，需要对每种符合范围的物品找一个最近的做setting
                foreach (var state in stackData.CurrentStates)
                {
                    if (state.ValueTrait != targetState.ValueTrait) continue;
                    if (settings.Any(setting => setting.ValueString.Equals(state.ValueString)))
                        continue;
                    settings.Add(state);
                }
            }
            return settings;
        }

        /// <summary>
        /// 条件：世界里要有对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        public void GetPreconditions([ReadOnly]ref State targetState, ref State setting,
            [ReadOnly]ref StackData stackData, ref StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                Trait = typeof(ItemContainerTrait),
                ValueTrait = setting.ValueTrait,
                ValueString = setting.ValueString,
            });
        }

        /// <summary>
        /// 效果：自身获得对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        public void GetEffects([ReadOnly]ref State targetState, ref State setting,
            [ReadOnly]ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(new State
            {
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = setting.ValueTrait,
                ValueString = setting.ValueString,
            });
        }
        
        public Entity GetNavigatingSubject(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            return preconditions[0].Target;
        }
    }
}