using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Entities;

namespace DOTS.Action
{
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
                //只针对自身stamina的正面goal state
                if (targetState.Target != stackData.AgentEntity) continue;
                if (targetState.Trait != typeof(StaminaTrait)) continue;
                if (!targetState.IsPositive) continue;

                return targetState;
            }

            return default;
        }

        public void GetPreconditions(ref State targetState, ref StackData stackData, ref StateGroup preconditions)
        {
            //自己有食物
            preconditions.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
                IsPositive = true
            });
            //世界里有餐桌
            var template = new State
            {
                SubjectType = StateSubjectType.Closest,    //寻找最近
                Target = Entity.Null,
                Trait = typeof(DiningTableTrait),
                IsPositive = true,
            };
            //todo 此处理应寻找最近目标，但目前的示例里没有transform系统，暂时直接用第一个合适的目标
            foreach (var currentState in stackData.CurrentStates)
            {
                if (currentState.Fits(template))
                {
                    preconditions.Add(currentState);
                    return;
                }
            }
        }

        public void GetEffects(ref State targetState, ref StackData stackData, ref StateGroup effects)
        {
            //自身获得stamina
            effects.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = stackData.AgentEntity,
                Trait = typeof(StaminaTrait),
                IsPositive = true
            });
        }

        public Entity GetNavigatingSubject(ref State targetState, ref StackData stackData,
            ref StateGroup preconditions)
        {
            //导航目标为餐桌
            return preconditions[1].Target;
        }
    }
}