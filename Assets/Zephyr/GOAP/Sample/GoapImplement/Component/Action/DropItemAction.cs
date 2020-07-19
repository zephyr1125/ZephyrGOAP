using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Sample.GoapImplement.Component.Action
{
    /// <summary>
    /// 每个setting表示一种适合的物品
    /// </summary>
    public struct DropItemAction : IComponentData, IAction
    {
        public int Level;
        
        public NativeString32 GetName()
        {
            return nameof(DropItemAction);
        }

        public bool CheckTargetRequire(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates)
        {
            //针对“目标获得物品”的state
            var stateFilter = new State
            {
                Trait = typeof(ItemDestinationTrait),
            };
            var agents = stackData.AgentEntities;
            //额外：target不能为agent
            return !agents.Contains(targetRequire.Target) && targetRequire.BelongTo(stateFilter);
        }
        
        public StateGroup GetSettings(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            
            if (!targetRequire.ValueString.Equals(default))
            {
                //如果指定了物品名，那么只有一种setting，也就是targetState本身
                settings.Add(targetRequire);
            }else if (targetRequire.ValueString.Equals(default) &&
                      targetRequire.ValueTrait != null)
            {
                //如果targetState是类别范围，需要对每种符合范围的物品做setting
                //todo 此处应查询define获得所有符合范围的物品名，示例里暂时从工具方法获取
                var itemNames =
                    Utils.GetItemNamesOfSpecificTrait(targetRequire.ValueTrait,
                        Allocator.Temp);
                for (var i = 0; i < itemNames.Length; i++)
                {
                    var state = targetRequire;
                    state.ValueString = itemNames[i];
                    settings.Add(state);
                }

                itemNames.Dispose();
            }
            return settings;
        }

        public void GetPreconditions(State targetRequire, Entity agentEntity, State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, StateGroup preconditions)
        {
            //我自己需要有指定的物品
            var state = setting;
            state.Target = agentEntity;
            state.Trait = typeof(ItemTransferTrait);
            preconditions.Add(state);
        }

        public void GetEffects(State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward(State targetRequire, State setting, [ReadOnly]StackData stackData)
        {
            return 0;
        }

        public float GetExecuteTime(State targetRequire, State setting, [ReadOnly]StackData stackData)
        {
            return 0;
        }

        public void GetNavigatingSubjectInfo(State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.EffectTarget;
            subjectId = 0;
        }
    }
}