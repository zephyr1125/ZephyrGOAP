using Unity.Collections;
using Unity.Entities;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Action
{
    /// <summary>
    /// 每个setting表示一种适合的原料
    /// </summary>
    public struct DropRawAction : IComponentData, IAction
    {
        public NativeString64 GetName()
        {
            return new NativeString64(nameof(DropRawAction));
        }

        public State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData)
        {
            //针对“目标获得原料”的state
            var stateFilter = new State
            {
                Trait = typeof(RawDestinationTrait),
            };
            var agent = stackData.AgentEntity;
            //额外：target不能为自身
            return targetStates.GetState(state => state.Target != agent && state.BelongTo(stateFilter));
        }
        
        public StateGroup GetSettings(ref State targetState, ref StackData stackData, Allocator allocator)
        {
            //目前不考虑无Target或宽泛类别的goal
            var settings = new StateGroup(1, allocator);
            
            Assert.IsFalse(targetState.ValueString.Equals(new NativeString64()));
            settings.Add(targetState);
            
            return settings;
        }

        public void GetPreconditions(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //我自己需要有指定的原料
            var state = setting;
            state.Target = stackData.AgentEntity;
            state.Trait = typeof(RawTransferTrait);
            preconditions.Add(state);
        }

        public void GetEffects(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
        }

        public Entity GetNavigatingSubject(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            return targetState.Target;
        }
    }
}