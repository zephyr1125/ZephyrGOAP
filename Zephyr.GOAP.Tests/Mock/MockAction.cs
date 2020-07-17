using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Tests.Mock
{
    public struct MockAction : IComponentData, IAction
    {
        public NativeString32 GetName()
        {
            return "MockAction";
        }

        public bool CheckTargetRequire(State targetRequire, Entity agentEntity, StackData stackData)
        {
            return true;
        }

        public StateGroup GetSettings(State targetRequire, Entity agentEntity, StackData stackData, Allocator allocator)
        {
            return new StateGroup(1, allocator);
        }

        public void GetPreconditions(State targetRequire, Entity agentEntity, State setting, StackData stackData,
            StateGroup preconditions)
        {
            
        }

        public void GetEffects(State targetRequire, State setting, StackData stackData,
            StateGroup effects)
        {
            
        }

        public float GetReward(State targetRequire, State setting, StackData stackData)
        {
            return 0;
        }

        public float GetExecuteTime(State targetRequire, State setting, StackData stackData)
        {
            return 0;
        }

        public void GetNavigatingSubjectInfo(State targetRequire, State setting, StackData stackData,
            StateGroup preconditions, out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.Null;
            subjectId = 0;
        }
    }
}