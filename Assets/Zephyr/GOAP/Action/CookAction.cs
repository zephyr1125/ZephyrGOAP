using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Action
{
    /// <summary>
    /// cook的setting为各种可烹饪食物
    /// </summary>
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
                //这是因为在指定物品名的情况下，有可能会省略ValueTrait
                if (!IsItemInRecipes(targetState.ValueString, ref stackData)) continue;
                
                return targetState;
            }

            return default;
        }

        /// <summary>
        /// 查询指定物品是否可以被生产
        /// (存在以他为输出的配方)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="stackData"></param>
        /// <returns></returns>
        private bool IsItemInRecipes(NativeString64 name, ref StackData stackData)
        {
            var foodRecipeState = new State
            {
                Trait = typeof(RecipeOutputTrait),
                ValueTrait = typeof(CookerTrait),
                ValueString = name
            };
            return !stackData.CurrentStates.GetBelongingState(foodRecipeState).Equals(State.Null);
        }
        
        public StateGroup GetSettings(ref State targetState, ref StackData stackData, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            
            if (!targetState.ValueString.Equals(new NativeString64()))
            {
                //如果指定了物品名，那么只有一种setting，也就是targetState本身
                settings.Add(targetState);
            }else if (targetState.ValueString.Equals(new NativeString64()) &&
                      targetState.ValueTrait == typeof(FoodTrait))
            {
                //如果targetState是类别范围，需要对每种符合范围的物品做setting
                //todo 此处应查询define获得所有符合范围的物品名，示例里暂时从工具方法获取
                var itemNames =
                    Utils.GetItemNamesOfSpecificTrait(targetState.ValueTrait,
                        Allocator.Temp);
                //还需要筛除不能被制作的item
                for (var i = itemNames.Length - 1; i >= 0; i--)
                {
                    if (!IsItemInRecipes(itemNames[i], ref stackData))
                    {
                        itemNames.RemoveAtSwapBack(i);
                    }
                }
                for (var i = 0; i < itemNames.Length; i++)
                {
                    var state = targetState;
                    state.ValueString = itemNames[i];
                    settings.Add(state);
                }

                itemNames.Dispose();
            }
            else
            {
                //都不符合就出错了
                Debug.LogError("wrong target in CookAction : "+targetState);
            }

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
                ValueString = setting.ValueString,
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
            effects.Add(setting);
        }

        public float GetReward(ref State targetState, ref State setting, ref StackData stackData)
        {
            return -1;
        }

        public Entity GetNavigatingSubject(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //移动目标为生产设施
            return preconditions[0].Target;
        }
    }
}