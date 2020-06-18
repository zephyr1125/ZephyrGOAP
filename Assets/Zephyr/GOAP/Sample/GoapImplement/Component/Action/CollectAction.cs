using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Sample.GoapImplement.Component.Action
{
    /// <summary>
    /// 原料收集
    /// </summary>
    public struct CollectAction : IComponentData, IAction
    {
        public int Level;
        
        public NativeString32 GetName()
        {
            return nameof(CollectAction);
        }

        public State GetTargetGoalState(ref StateGroup targetStates, ref StackData stackData)
        {
            foreach (var targetState in targetStates)
            {
                var itemSourceState = new State
                {
                    Trait = typeof(ItemSourceTrait),
                };
                //只针对物品源需求的goal state
                if (!targetState.BelongTo(itemSourceState)) continue;
                //不支持没有value string
                if (targetState.ValueString.Equals(default)) continue;
                //如果Target已明确，那么Target必须是Collector
                if (targetState.Target != Entity.Null)
                {
                    var collectorTemplate = new State
                    {
                        Target = targetState.Target,
                        Trait = typeof(CollectorTrait)
                    };
                    var foundState = stackData.CurrentStates.GetBelongingState(collectorTemplate);
                    if (foundState.Equals(State.Null)) continue;
                }
                
                return targetState;
            }

            return default;
        }
        
        public StateGroup GetSettings(ref State targetState, ref StackData stackData, Allocator allocator)
        {
            var settings = new StateGroup(1, allocator);

            //如果没有指定目标，那么目前只考虑一种setting，即距离最近的能够采到目标物的collector
            if (targetState.Target == Entity.Null)
            {
                //寻找能够采集的最近的collector
                var collectorState = new State
                {
                    Trait = typeof(CollectorTrait)
                };
                var collectors =
                    stackData.CurrentStates.GetBelongingStates(collectorState, Allocator.Temp);
                var nearestCollectorState = default(State);
                var nearestDistance = float.MaxValue;
                foreach (var collector in collectors)
                {
                    var collectState = new State
                    {
                        Target = collector.Target,
                        Trait = typeof(ItemPotentialSourceTrait),
                        ValueString = targetState.ValueString
                    };
                    if(stackData.CurrentStates.GetBelongingState(collectState).Equals(default))continue;
                    var distance = math.distance(collector.Position,
                        stackData.AgentPositions[stackData.CurrentAgentId]);
                    if (distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearestCollectorState = collector;
                }

                if (!nearestCollectorState.Equals(default))
                {
                    targetState.Target = nearestCollectorState.Target;
                    targetState.Position = nearestCollectorState.Position;
                };
                
                collectors.Dispose();
            }
            if(targetState.Target!=Entity.Null)settings.Add(targetState);
            return settings;
        }

        public void GetPreconditions(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                Target = setting.Target,
                Position = setting.Position,
                Trait = typeof(RawDestinationTrait),
                ValueString = setting.ValueString
            });
        }

        public void GetEffects(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(setting);
        }

        public float GetReward(ref State targetState, ref State setting, ref StackData stackData)
        {
            return -1;
        }

        public float GetExecuteTime(ref State targetState, ref State setting, ref StackData stackData)
        {
            return 4 / (float)(Level + 1);
        }

        public void GetNavigatingSubjectInfo(ref State targetState, ref State setting,
            ref StackData stackData, ref StateGroup preconditions,
            out NodeNavigatingSubjectType subjectType, out byte subjectId)
        {
            //移动目标为采集设施
            subjectType = NodeNavigatingSubjectType.PreconditionTarget;
            subjectId = 0;
        }
    }
}