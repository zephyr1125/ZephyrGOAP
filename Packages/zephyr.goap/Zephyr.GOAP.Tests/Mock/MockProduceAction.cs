using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Tests.Mock
{
    //effect 为 mock A 1个，precondition 为 mock B 2个
    public struct MockProduceAction : IComponentData, IAction
    {
        public int Level;
        
        public bool CheckTargetRequire(State targetRequire, Entity agentEntity
            , [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates)
        {
            if (targetRequire.Amount == 0) return false;
            
            var template = new State
            {
                Trait = TypeManager.GetTypeIndex<MockTraitA>()
            };
            return targetRequire.BelongTo(template);
        }

        public StateGroup GetSettings(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            settings.Add(targetRequire);
            return settings;
        }

        public void GetPreconditions(State targetRequire, Entity agentEntity, State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, StateGroup preconditions)
        {
            var precondition = setting;
            precondition.Trait = TypeManager.GetTypeIndex<MockTraitB>();
            precondition.Amount = 2;
            preconditions.Add(precondition);
        }

        public void GetEffects(State targetRequire, State setting, [ReadOnly]StackData stackData,
            StateGroup effects)
        {
            var effect = setting;
            effect.Amount = 1;
            effects.Add(effect);
        }

        public float GetReward(State targetRequire, State setting, [ReadOnly]StackData stackData)
        {
            return 0;
        }

        public float GetExecuteTime([ReadOnly]State setting)
        {
            return 0;
        }

        public void GetNavigatingSubjectInfo(State targetRequire, State setting, [ReadOnly]StackData stackData,
            StateGroup preconditions, out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.Null;
            subjectId = 0;
        }
    }
}