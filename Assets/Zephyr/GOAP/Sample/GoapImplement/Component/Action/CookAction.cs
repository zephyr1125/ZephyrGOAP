using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Sample.GoapImplement.Component.Action
{
    /// <summary>
    /// cook的setting为各种可烹饪食物
    /// </summary>
    public struct CookAction : IComponentData, IAction
    {
        public int Level;

        public bool CheckTargetRequire(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates)
        {
            //数量应该大于0
            if (targetRequire.Amount == 0) return false;
                
            var itemSourceState = new State
            {
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
            };
            //只针对物品源需求的goal state
            if (!targetRequire.BelongTo(itemSourceState)) return false;
                
            //如果targetState有指明物品名，则直接寻找其是否为cooker的产物
            //这是因为在指定物品名的情况下，有可能会省略ValueTrait
            if (!targetRequire.ValueString.Equals(new FixedString32())
                &&!IsItemInRecipes(targetRequire.ValueString, currentStates)) return false;
                
            //如果没有指定物品名，则必须指定FoodTrait
            if (targetRequire.ValueString.Equals(new FixedString32()) &&
                targetRequire.ValueTrait != TypeManager.GetTypeIndex<FoodTrait>()) return false;
                
            return true;
        }

        /// <summary>
        /// 查询指定物品是否可以被生产
        /// (存在以他为输出的配方)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="currentStates"></param>
        /// <returns></returns>
        private bool IsItemInRecipes(FixedString32 name, [ReadOnly]StateGroup currentStates)
        {
            var foodRecipeState = new State
            {
                Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                ValueString = name
            };
            return !currentStates.GetBelongingState(foodRecipeState).Equals(default);
        }
        
        public StateGroup GetSettings(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);

            if (targetRequire.Target == Entity.Null)
            {
                //首先寻找最近的Cooker，如果没有则没有setting
                var cookerState = new State {Trait = TypeManager.GetTypeIndex<CookerTrait>()};
                var cookerStates = currentStates.GetBelongingStates(cookerState, Allocator.Temp);
                if (cookerStates.Length() <= 0)
                {
                    cookerStates.Dispose();
                    return settings;
                }

                var nearestDistance = float.MaxValue;
                var nearestState = new State();
                var agentPosition = stackData.GetAgentPosition(agentEntity);
                for (var stateId = 0; stateId < cookerStates.Length(); stateId++)
                {
                    var state = cookerStates[stateId];
                    var distance = math.distance(state.Position, agentPosition);
                    if (!(distance < nearestDistance)) continue;
                    nearestDistance = distance;
                    nearestState = state;
                }

                //todo 将来还需要考虑排除忙碌的cooker
                targetRequire.Target = nearestState.Target;
                targetRequire.Position = nearestState.Position;
            
                cookerStates.Dispose();
            }

            if (!targetRequire.ValueString.Equals(new FixedString32()))
            {
                //如果指定了物品名，那么只有一种setting，也就是targetState本身
                settings.Add(targetRequire);
            }else if (targetRequire.ValueString.Equals(new FixedString32()) &&
                      targetRequire.ValueTrait == TypeManager.GetTypeIndex<FoodTrait>())
            {
                //如果targetState是类别范围，需要对每种符合范围的物品做setting
                //todo 此处应查询define获得所有符合范围的物品名，示例里暂时从工具方法获取
                var itemNames =
                    Utils.GetItemNamesOfSpecificTrait(targetRequire.ValueTrait,
                        stackData.ItemNames, Allocator.Temp);
                //还需要筛除不能被制作的item
                for (var i = itemNames.Length - 1; i >= 0; i--)
                {
                    if (!IsItemInRecipes(itemNames[i], currentStates))
                    {
                        itemNames.RemoveAtSwapBack(i);
                    }
                }
                for (var i = 0; i < itemNames.Length; i++)
                {
                    var state = targetRequire;
                    state.ValueString = itemNames[i];
                    settings.Add(state);
                }

                itemNames.Dispose();
            }
            else
            {
                //都不符合就出错了
                Debug.LogError($"wrong target in CookAction : {targetRequire}");
            }

            return settings;
        }

        public void GetPreconditions(State targetRequire, Entity agentEntity, State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, StateGroup preconditions)
        {
            //cooker有其生产所需原料
            var targetRecipeOutputFilter = new State
            {
                Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                ValueString = setting.ValueString,
                Amount = setting.Amount
            };
            var inputs = Utils.GetRecipeInputInStateGroup(currentStates,
                targetRecipeOutputFilter, Allocator.Temp, out var outputAmount);
            //把查到的配方转化为对此设施拥有的需求
            preconditions.Add(new State
            {
                Target = setting.Target,    //Target仍是同一设施
                Position = setting.Position,
                Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                ValueString = inputs[0].ValueString,
                Amount = inputs[0].Amount
            });
            if (!inputs[1].Equals(State.Null))
            {
                preconditions.Add(new State
                {
                    Target = setting.Target,
                    Position = setting.Position,
                    Trait = TypeManager.GetTypeIndex<ItemDestinationTrait>(),
                    ValueString = inputs[1].ValueString,
                    Amount = inputs[1].Amount
                });
            }
            inputs.Dispose();
        }

        public void GetEffects(State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup effects)
        {
            //设施拥有了cook产物
            effects.Add(setting);
        }

        public float GetReward(State targetRequire, State setting, [ReadOnly]StackData stackData)
        {
            return -1;
        }

        public float GetExecuteTime([ReadOnly]State setting)
        {
            return 4 / (float) (Level + 1);
        }

        public void GetNavigatingSubjectInfo(State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup preconditions,
                out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            //移动目标为生产设施
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}