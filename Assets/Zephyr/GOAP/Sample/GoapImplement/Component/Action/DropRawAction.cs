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

        public bool CheckTargetRequire(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates)
        {
            //数量应该大于0
            if (targetRequire.Amount == 0) return false;
            
            //针对“目标获得原料”的state
            var stateFilter = new State
            {
                Trait = TypeManager.GetTypeIndex<RawDestinationTrait>(),
            };
            var agents = stackData.AgentEntities;
            //额外：target不能为自身
            return !agents.Contains(targetRequire.Target) && targetRequire.BelongTo(stateFilter);
        }
        
        public StateGroup GetSettings(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, Allocator allocator)
        {
            //目前不考虑无Target或宽泛类别的goal
            var settings = new StateGroup(1, allocator);
            
            Assert.IsFalse(targetRequire.ValueString.Equals(new FixedString32()));
            settings.Add(targetRequire);
            
            return settings;
        }

        public void GetPreconditions(State targetRequire, Entity agentEntity, State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, StateGroup preconditions)
        {
            //我自己需要有指定的原料
            var state = setting;
            state.Target = agentEntity;
            state.Trait = TypeManager.GetTypeIndex<RawTransferTrait>();
            preconditions.Add(state);
        }

        public void GetEffects(State targetRequire, State setting,
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
            return 0;
        }

        public void GetNavigatingSubjectInfo(State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.EffectTarget;
            subjectId = 0;
        }
    }
}