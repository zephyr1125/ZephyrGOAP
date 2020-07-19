using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Sample.GoapImplement.Component.Action
{
    /// <summary>
    /// Eat的setting为各种食物的餐桌，用于precondition
    /// </summary>
    public struct EatAction : IComponentData, IAction
    {
        public int Level;
        
        public NativeString32 GetName()
        {
            return nameof(EatAction);
        }

        public bool CheckTargetRequire(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates)
        {
            var staminaState = new State
            {
                Target = agentEntity,
                Trait = typeof(StaminaTrait),
            };

            return targetRequire.BelongTo(staminaState);
        }
        
        public StateGroup GetSettings(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            
            //setting为有食物的餐桌
            var diningTableTemplate = new State
            {
                Trait = typeof(DiningTableTrait),
            };
            var tables =
                currentStates.GetBelongingStates(diningTableTemplate, Allocator.Temp);
            var itemNames =
                Utils.GetItemNamesOfSpecificTrait(typeof(FoodTrait),
                    Allocator.Temp);
            //todo 此处考虑直接寻找最近的餐桌以避免setting过于膨胀
            foreach (var table in tables)
            {
                for (var i = 0; i < itemNames.Length; i++)
                {
                    var itemName = itemNames[i];
                    var foodOnTableTemplate = new State
                    {
                        Target = table.Target,
                        Trait = typeof(ItemDestinationTrait),
                        ValueString = itemName,
                        Amount = 1
                    };
                    settings.Add(foodOnTableTemplate);
                }
            }
            
            itemNames.Dispose();
            tables.Dispose();

            return settings;
        }

        public void GetPreconditions(State targetRequire, Entity agentEntity, State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, StateGroup preconditions)
        {
            //有食物的餐桌
            preconditions.Add(setting);
        }

        public void GetEffects(State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup effects)
        {
            //自身获得stamina
            effects.Add(targetRequire);
        }

        public float GetReward(State targetRequire, State setting, [ReadOnly]StackData stackData)
        {
            //由食物决定
            //todo 示例项目通过工具方法获取食物reward，实际应从define取
            return Utils.GetFoodReward(setting.ValueString);
        }

        public float GetExecuteTime(State targetRequire, State setting, [ReadOnly]StackData stackData)
        {
            return 2;
        }

        public void GetNavigatingSubjectInfo(State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            //导航目标为餐桌
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}