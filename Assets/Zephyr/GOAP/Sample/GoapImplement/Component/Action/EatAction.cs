using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.Trait;
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

        public State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData)
        {
            foreach (var targetState in targetStates)
            {
                var staminaState = new State
                {
                    Target = stackData.AgentEntities[stackData.CurrentAgentId],
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
            
            //setting为有食物的餐桌
            var diningTableTemplate = new State
            {
                Trait = typeof(DiningTableTrait),
            };
            var tables =
                stackData.CurrentStates.GetBelongingStates(diningTableTemplate, Allocator.Temp);
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
                        ValueString = itemName
                    };
                    settings.Add(foodOnTableTemplate);
                }
            }
            
            itemNames.Dispose();
            tables.Dispose();

            return settings;
        }

        public void GetPreconditions(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //有食物的餐桌
            preconditions.Add(setting);
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

        public float GetExecuteTime(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 2;
        }

        public void GetNavigatingSubjectInfo(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            //导航目标为餐桌
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}