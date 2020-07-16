using Unity.Collections;
using Unity.Entities;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Sample.GoapImplement.Component.Action
{
    /// <summary>
    /// 每个setting表示一种适合的原料
    /// </summary>
    public struct DropRawAction : IComponentData, IAction
    {
        public int Level;
        
        public NativeString32 GetName()
        {
            return nameof(DropRawAction);
        }

        public State GetTargetRequire(ref StateGroup targetRequires, Entity agentEntity, ref StackData stackData)
        {
            //针对“目标获得原料”的state
            var stateFilter = new State
            {
                Trait = typeof(RawDestinationTrait),
            };
            var agents = stackData.AgentEntities;
            //额外：target不能为自身
            return targetRequires.GetState(state => !agents.Contains(state.Target) && state.BelongTo(stateFilter));
        }
        
        public StateGroup GetSettings(ref State targetState, Entity agentEntity, ref StackData stackData, Allocator allocator)
        {
            //目前不考虑无Target或宽泛类别的goal
            var settings = new StateGroup(1, allocator);
            
            Assert.IsFalse(targetState.ValueString.Equals(default));
            settings.Add(targetState);
            
            return settings;
        }

        public void GetPreconditions(ref State targetState, Entity agentEntity, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //我自己需要有指定的原料
            var state = setting;
            state.Target = agentEntity;
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

        public float GetExecuteTime(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
        }

        public void GetNavigatingSubjectInfo(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.EffectTarget;
            subjectId = 0;
        }
    }
}