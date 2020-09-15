using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Sample.GoapImplement.Component.Action
{
    /// <summary>
    /// 原料收集
    /// </summary>
    public struct CollectAction : IComponentData, IAction
    {
        public int Level;

        public bool CheckTargetRequire(State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates)
        {
            //要有数量
            if (targetRequire.Amount == 0) return false;
            
            var itemSourceState = new State
            {
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>()
            };
            //只针对物品源需求的goal state
            if (!targetRequire.BelongTo(itemSourceState)) return false;
            //不支持没有value string
            if (targetRequire.ValueString.Equals(new FixedString32())) return false;
            //如果Target已明确，那么Target必须是一个现存的Collector
            if (targetRequire.Target != Entity.Null)
            {
                var collectorTemplate = new State
                {
                    Target = targetRequire.Target,
                    Trait = TypeManager.GetTypeIndex<CollectorTrait>()
                };
                var foundState = currentStates.GetBelongingState(collectorTemplate);
                if (foundState.Equals(default)) return false;
            }
                
            return true;
        }
        
        public StateGroup GetSettings([ReadOnly]State targetRequire, Entity agentEntity,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);
            var settingRequire = targetRequire;

            //如果没有指定目标，那么目前只考虑一种setting，即距离最近的能够采到目标物的collector
            if (settingRequire.Target == Entity.Null)
            {
                //寻找能够采集的最近的collector
                var collectorState = new State
                {
                    Trait = TypeManager.GetTypeIndex<CollectorTrait>()
                };
                var collectors =
                    currentStates.GetBelongingStates(collectorState, Allocator.Temp);
                var nearestCollectorState = default(State);
                var nearestDistance = float.MaxValue;
                var nearestAmount = 0;
                for (var collectorId = 0; collectorId < collectors.Length(); collectorId++)
                {
                    var collector = collectors[collectorId];
                    var collectState = new State
                    {
                        Target = collector.Target,
                        Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                        ValueString = settingRequire.ValueString
                    };
                    var existedSource = currentStates.GetBelongingState(collectState);
                    if(existedSource.Equals(default))continue;
                    var distance = math.distance(collector.Position,
                        stackData.GetAgentPosition(agentEntity));
                    if (distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearestCollectorState = collector;
                    nearestAmount = existedSource.Amount;
                }

                if (!nearestCollectorState.Equals(default))
                {
                    settingRequire.Target = nearestCollectorState.Target;
                    settingRequire.Position = nearestCollectorState.Position;
                    if (settingRequire.Amount > nearestAmount) settingRequire.Amount = nearestAmount;
                }
                
                collectors.Dispose();
            }
            //没有collector或者数量为0时，都没有setting
            if(settingRequire.Target!=Entity.Null && settingRequire.Amount > 0)settings.Add(settingRequire);
            return settings;
        }

        public void GetPreconditions([ReadOnly]State targetRequire, Entity agentEntity, [ReadOnly]State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup currentStates, [ReadOnly]StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                Target = setting.Target,
                Position = setting.Position,
                Trait = TypeManager.GetTypeIndex<RawDestinationTrait>(),
                ValueString = setting.ValueString,
                Amount = setting.Amount
            });
            //额外增加一个ItemPotential的precondition是为了delta能够计入ItemPotential的减少
            preconditions.Add(new State
            {
                Target = setting.Target,
                Position = setting.Position,
                Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                ValueString = setting.ValueString,
                Amount = setting.Amount
            });
        }

        public void GetEffects([ReadOnly]State targetRequire, [ReadOnly]State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward([ReadOnly]State targetRequire, [ReadOnly]State setting, [ReadOnly]StackData stackData)
        {
            return -1;
        }

        public float GetExecuteTime([ReadOnly]State setting)
        {
            return 0;
        }

        public void GetNavigatingSubjectInfo([ReadOnly]State targetRequire, [ReadOnly]State setting,
            [ReadOnly]StackData stackData, [ReadOnly]StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            //移动目标为采集设施
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}