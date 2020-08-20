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
        
        public bool CheckTargetRequire(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates)
        {
            //数量应该大于0
            if (targetRequire.Amount == 0) return false;
            
            //针对“自身获得原料”的state
            var stateFilter = new State
            {
                Target = agentEntity,
                Trait = TypeManager.GetTypeIndex<RawTransferTrait>(),
            };
            return targetRequire.BelongTo(stateFilter);
        }

        public StateGroup GetSettings(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, Allocator allocator)
        {
            //不考虑无Target或宽泛类别的goal
            Assert.IsFalse(targetRequire.ValueString.Equals(new FixedString32()));
           
            //setting直接就是targetRequire本身
            return new StateGroup(1, allocator) {targetRequire};
        }

        /// <summary>
        /// 条件：世界里要有对应原料
        /// </summary>
        /// <param name="targetRequire"></param>
        /// <param name="agentEntity"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="currentStates"></param>
        /// <param name="preconditions"></param>
        public void GetPreconditions([ReadOnly]State targetRequire, Entity agentEntity, State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, StateGroup preconditions)
        {
            var state = setting;
            state.Target = Entity.Null;
            state.Trait = TypeManager.GetTypeIndex<RawSourceTrait>();
            preconditions.Add(state);
        }

        /// <summary>
        /// 效果：自身获得对应原料
        /// </summary>
        /// <param name="targetRequire"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        public void GetEffects([ReadOnly]State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward(State targetRequire, State setting, [ReadOnly]StackData stackData)
        {
            return 0;
        }

        public float GetExecuteTime([ReadOnly]State setting)
        {
            return 1f;
        }

        public void GetNavigatingSubjectInfo(State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}