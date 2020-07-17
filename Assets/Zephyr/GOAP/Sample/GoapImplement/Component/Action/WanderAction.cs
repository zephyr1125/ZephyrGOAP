using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;

// using Zephyr.GOAP.System.ActionExecuteSystem;

namespace Zephyr.GOAP.Sample.GoapImplement.Component.Action
{
    /// <summary>
    /// 闲逛一段时间
    /// </summary>
    public struct WanderAction : IComponentData, IAction
    {
        public int Level;
        
        public NativeString32 GetName()
        {
            return nameof(WanderAction);
        }

        public bool CheckTargetRequire(State targetRequire, Entity agentEntity, [ReadOnly]StackData stackData)
        {
            var wanderState = new State
            {
                Target = agentEntity,
                Trait = typeof(WanderTrait),
            };
            
            //只针对自身wander
            return targetRequire.BelongTo(wanderState);
        }
        
        public StateGroup GetSettings(ref State targetState, Entity agentEntity, ref StackData stackData, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            
            settings.Add(targetState);

            return settings;
        }

        public void GetPreconditions(ref State targetState, Entity agentEntity, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //没有precondition
        }

        public void GetEffects(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup effects)
        {
            //达成wander需求
            effects.Add(setting);
        }

        public float GetReward(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
        }

        public float GetExecuteTime(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
            // return WanderActionExecuteSystem.WanderTime;
        }

        public void GetNavigatingSubjectInfo(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.Null;
            subjectId = 0;
        }
    }
}