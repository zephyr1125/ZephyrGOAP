using DOTS.Component.Trait;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Action
{
    public struct CookAction : IComponentData, IAction
    {
        public NativeString64 GetName()
        {
            return new NativeString64(nameof(CookAction));
        }

        public State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData)
        {
            foreach (var targetState in targetStates)
            {
                var foodState = new State
                {
                    Target = stackData.AgentEntity,
                    Trait = typeof(ItemContainerTrait),
                    ValueTrait = typeof(FoodTrait),
                };
                //只针对自身食物类物品需求的goal state
                if (!targetState.BelongTo(foodState)) continue;
                
                //如果targetState有指明物品名，则直接寻找其是否为cooker的产物
                //todo 而如果targetState没有指明食品名，则需要产生多种setting（目前只有一个setting，不够完备
                var foodRecipeState = new State
                {
                    Trait = typeof(RecipeOutputTrait),
                    ValueTrait = typeof(CookerTrait),
                    ValueString = targetState.ValueString, //具体产物名要来自于targetState，可能不指明也可能指明
                };
                if(stackData.CurrentStates.GetBelongingState(foodRecipeState).Equals(State.Null)) continue;
                
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
            //世界里有cooker
            preconditions.Add(new State
            {
                Trait = typeof(CookerTrait),
            });
            
            //自己有其生产所需原料
            var targetRecipeInputFilter = new State
            {
                Trait = typeof(RecipeOutputTrait),
                ValueTrait = typeof(CookerTrait),
                ValueString = targetState.ValueString,
            };
            var inputs = Utils.GetRecipeInputInCurrentStates(ref stackData.CurrentStates,
                targetRecipeInputFilter, Allocator.Temp);
            //把查到的配方转化为对自己拥有的需求
            preconditions.Add(new State
            {
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueString = inputs[0].ValueString,
            });
            if (!inputs[1].Equals(State.Null))
            {
                preconditions.Add(new State
                {
                    Target = stackData.AgentEntity,
                    Trait = typeof(ItemContainerTrait),
                    ValueString = inputs[1].ValueString,
                });
            }
            inputs.Dispose();
        }

        public void GetEffects(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup effects)
        {
            //自己拥有了cook产物
            effects.Add(new State
            {
                Target = stackData.AgentEntity,
                Trait = typeof(ItemContainerTrait),
                ValueTrait = typeof(FoodTrait),
                ValueString = targetState.ValueString,
            });
        }

        public Entity GetNavigatingSubject(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //移动目标为生产设施
            return preconditions[0].Target;
        }
    }
}