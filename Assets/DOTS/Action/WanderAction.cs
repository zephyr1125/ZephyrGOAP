using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Action
{
    /// <summary>
    /// 闲逛一段时间
    /// </summary>
    public struct WanderAction : IComponentData, IAction
    {
        public NativeString64 GetName()
        {
            return new NativeString64(nameof(WanderAction));
        }

        public State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData)
        {
            foreach (var targetState in targetStates)
            {
                var wanderState = new State
                {
                    Target = stackData.AgentEntity,
                    Trait = typeof(WanderTrait),
                };
                //只针对自身wander
                if (!targetState.BelongTo(wanderState)) continue;

                return targetState;
            }

            return default;
        }
        
        public StateGroup GetSettings(ref State targetState, ref StackData stackData, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            
            settings.Add(targetState);

            return settings;
        }

        public void GetPreconditions(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //没有precondition
            preconditions.Add(new State());
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

        public Entity GetNavigatingSubject(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            return Entity.Null;
        }
    }
}