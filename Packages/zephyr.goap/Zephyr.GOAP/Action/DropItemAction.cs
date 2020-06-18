using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Action
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

        public State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData)
        {
            //针对“目标获得物品”的state
            var stateFilter = new State
            {
                Trait = typeof(ItemDestinationTrait),
            };
            var agents = stackData.AgentEntities;
            //额外：target不能为agent
            return targetStates.GetState(state => !agents.Contains(state.Target) && state.BelongTo(stateFilter));
        }
        
        public StateGroup GetSettings(ref State targetState, ref StackData stackData, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            
            if (!targetState.ValueString.Equals(default))
            {
                //如果指定了物品名，那么只有一种setting，也就是targetState本身
                settings.Add(targetState);
            }else if (targetState.ValueString.Equals(default) &&
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

        public void GetPreconditions(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            //我自己需要有指定的物品
            var state = setting;
            state.Target = stackData.AgentEntities[stackData.CurrentAgentId];
            state.Trait = typeof(ItemTransferTrait);
            preconditions.Add(state);
        }

        public void GetEffects(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
        }

        public float GetExecuteTime(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 0;
        }

        public void GetNavigatingSubjectInfo(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            subjectType = NodeNavigatingSubjectType.EffectTarget;
            subjectId = 0;
        }
    }
}