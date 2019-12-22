using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Action
{
    /// <summary>
    /// Eat的setting没有多重，就是自身获得Stamina
    /// </summary>
    public struct EatAction : IComponentData, IAction
    {
        public NativeString64 GetName()
        {
            return new NativeString64(nameof(EatAction));
        }

        public State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData)
        {
            foreach (var targetState in targetStates)
            {
                var staminaState = new State
                {
                    Target = stackData.AgentEntity,
                    Trait = typeof(StaminaTrait),
                };
                //只针对自身stamina的正面goal state
                if (!targetState.BelongTo(staminaState)) continue;

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
            //自己有食物
            preconditions.Add(new State
            {
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
            });
            //世界里有餐桌
            preconditions.Add(new State
            {
                Trait = typeof(DiningTableTrait),
            });
        }

        public void GetEffects(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup effects)
        {
            //自身获得stamina
            effects.Add(targetState);
        }

        public float GetReward(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
        }

        public Entity GetNavigatingSubject(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //导航目标为餐桌
            return preconditions[1].Target;
        }
    }
}