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
    public struct PickItemAction : IComponentData, IAction
    {
        public int Level;
        
        public NativeString32 GetName()
        {
            return nameof(PickItemAction);
        }
        
        public bool CheckTargetRequire(State targetRequire, Entity agentEntity, [ReadOnly]StackData stackData)
        {
            //针对“自身运输物品”的state
            var stateFilter = new State
            {
                Target = agentEntity,
                Trait = typeof(ItemTransferTrait),
            };
            return targetRequire.BelongTo(stateFilter);
        }

        public StateGroup GetSettings(State targetRequire, Entity agentEntity, StackData stackData, Allocator allocator)
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

        /// <summary>
        /// 条件：世界里要有对应物品
        /// </summary>
        /// <param name="targetRequire"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        public void GetPreconditions([ReadOnly]State targetRequire, Entity agentEntity, State setting,
            [ReadOnly]StackData stackData, StateGroup preconditions)
        {
            var state = setting;
            state.Target = Entity.Null;
            state.Trait = typeof(ItemSourceTrait);
            preconditions.Add(state);
        }

        /// <summary>
        /// 效果：自身获得对应物品
        /// </summary>
        /// <param name="targetRequire"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        public void GetEffects([ReadOnly]State targetRequire, State setting,
            [ReadOnly]StackData stackData, StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward(State targetRequire, State setting, StackData stackData)
        {
            return 0;
        }

        public float GetExecuteTime(State targetRequire, State setting, StackData stackData)
        {
            return 0.5f;
        }

        public void GetNavigatingSubjectInfo(State targetRequire, State setting,
            StackData stackData, StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}