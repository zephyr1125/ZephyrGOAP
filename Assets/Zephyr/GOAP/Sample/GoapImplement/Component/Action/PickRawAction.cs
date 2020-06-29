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
    public struct PickRawAction : IComponentData, IAction
    {
        public int Level;
        
        public NativeString32 GetName()
        {
            return nameof(PickRawAction);
        }
        
        public State GetTargetGoalState([ReadOnly]ref StateGroup targetStates,
            [ReadOnly]ref StackData stackData)
        {
            //针对“自身获得原料”的state
            var stateFilter = new State
            {
                Target = stackData.AgentEntities[stackData.CurrentAgentId],
                Trait = typeof(RawTransferTrait),
            };
            return targetStates.GetBelongingState(stateFilter);
        }

        public StateGroup GetSettings(ref State targetState, ref StackData stackData, Allocator allocator)
        {
            //目前不考虑无Target或宽泛类别的goal
            var settings = new StateGroup(1, allocator);
            
            Assert.IsFalse(targetState.ValueString.Equals(default));
            settings.Add(targetState);
            
            return settings;
        }

        /// <summary>
        /// 条件：世界里要有对应原料
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        public void GetPreconditions([ReadOnly]ref State targetState, ref State setting,
            [ReadOnly]ref StackData stackData, ref StateGroup preconditions)
        {
            var state = setting;
            state.Target = Entity.Null;
            state.Trait = typeof(RawSourceTrait);
            preconditions.Add(state);
        }

        /// <summary>
        /// 效果：自身获得对应原料
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        public void GetEffects([ReadOnly]ref State targetState, ref State setting,
            [ReadOnly]ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
        }

        public float GetExecuteTime(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0.5f;
        }

        public void GetNavigatingSubjectInfo(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}