using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Action
{
    /// <summary>
    /// Eat的setting为自己拥有各种食物，用于precondition
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
            
            //自己有食物
            var template = new State
            {
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
            };
            
            //todo 此处应查询define获得所有食物，示例里暂时从工具方法获取
            var itemNames =
                Utils.GetItemNamesOfSpecificTrait(typeof(FoodTrait),
                    Allocator.Temp);
            for (var i = 0; i < itemNames.Length; i++)
            {
                var state = template;
                state.ValueString = itemNames[i];
                settings.Add(state);
            }

            itemNames.Dispose();

            return settings;
        }

        public void GetPreconditions(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //自己有食物
            preconditions.Add(setting);
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
            //由食物决定
            //todo 示例项目通过工具方法获取食物reward，实际应从define取
            return Utils.GetFoodReward(setting.ValueString);
        }

        public Entity GetNavigatingSubject(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //导航目标为餐桌
            return preconditions[1].Target;
        }
    }
}