using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Action
{
    /// <summary>
    /// 每个setting表示一种适合的物品
    /// </summary>
    public struct PickItemAction : IComponentData, IAction
    {
        public NativeString64 GetName()
        {
            return new NativeString64(nameof(PickItemAction));
        }
        
        public State GetTargetGoalState([ReadOnly]ref StateGroup targetStates,
            [ReadOnly]ref StackData stackData)
        {
            //针对“自身运输物品”的state
            var stateFilter = new State
            {
                Target = stackData.AgentEntities[stackData.CurrentAgentId],
                Trait = typeof(ItemTransferTrait),
            };
            return targetStates.GetBelongingState(stateFilter);
        }

        public StateGroup GetSettings(ref State targetState, ref StackData stackData, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            
            if (!targetState.ValueString.Equals(new NativeString64()))
            {
                //如果指定了物品名，那么只有一种setting，也就是targetState本身
                settings.Add(targetState);
            }else if (targetState.ValueString.Equals(new NativeString64()) &&
                      targetState.ValueTrait != null)
            {
                //如果targetState是类别范围，需要对每种符合范围的物品做setting
                //todo 此处应查询define获得所有符合范围的物品名，示例里暂时从工具方法获取
                var itemNames =
                    Utils.GetItemNamesOfSpecificTrait(targetState.ValueTrait,
                        Allocator.Temp);
                for (var i = 0; i < itemNames.Length; i++)
                {
                    var state = targetState;
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
        /// <param name="targetState"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        public void GetPreconditions([ReadOnly]ref State targetState, ref State setting,
            [ReadOnly]ref StackData stackData, ref StateGroup preconditions)
        {
            var state = setting;
            state.Target = Entity.Null;
            state.Trait = typeof(ItemSourceTrait);
            preconditions.Add(state);
        }

        /// <summary>
        /// 效果：自身获得对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="setting"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        public void GetEffects([ReadOnly]ref State targetState, ref State setting,
            [ReadOnly]ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
        }

        public void GetNavigatingSubjectInfo(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}