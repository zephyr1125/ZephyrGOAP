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
            var itemSourceState = new State
            {
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>()
            };
            //只针对物品源需求的goal state
            if (!targetRequire.BelongTo(itemSourceState)) return false;
            //不支持没有value string
            if (targetRequire.ValueString.Equals(default)) return false;
            //如果Target已明确，那么Target必须是Collector
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
                for (var collectorId = 0; collectorId < collectors.Length(); collectorId++)
                {
                    var collector = collectors[collectorId];
                    var collectState = new State
                    {
                        Target = collector.Target,
                        Trait = TypeManager.GetTypeIndex<ItemPotentialSourceTrait>(),
                        ValueString = settingRequire.ValueString
                    };
                    if(currentStates.GetBelongingState(collectState).Equals(default))continue;
                    var distance = math.distance(collector.Position,
                        stackData.GetAgentPosition(agentEntity));
                    if (distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearestCollectorState = collector;
                }

                if (!nearestCollectorState.Equals(default))
                {
                    settingRequire.Target = nearestCollectorState.Target;
                    settingRequire.Position = nearestCollectorState.Position;
                };
                
                collectors.Dispose();
            }
            if(settingRequire.Target!=Entity.Null)settings.Add(settingRequire);
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
                ValueString = setting.ValueString
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

        public float GetExecuteTime([ReadOnly]State targetRequire, [ReadOnly]State setting, [ReadOnly]StackData stackData)
        {
            return 4 / (float)(Level + 1);
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