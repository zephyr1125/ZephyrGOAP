using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class StateLog
    {
        public EntityLog Target;
        public string Trait;
        public string ValueString;
        public string ValueTrait;
        public bool IsNegative;

        public StateLog(EntityManager entityManager, State state)
        {
            Target = new EntityLog(entityManager, state.Target);
            if(state.Trait!=default)Trait = state.Trait.ToString();
            ValueString = state.ValueString.ToString();
            if(state.ValueTrait!=default)ValueTrait = state.ValueTrait.ToString();
            IsNegative = state.IsNegative;
        }

        public static StateLog[] CreateStateViews(EntityManager entityManager, State[] states)
        {
            var stateViews = new List<StateLog>(states.Length);
            stateViews.AddRange(states.Select(t => new StateLog(entityManager, t)));
            return stateViews.ToArray();
        }

        public override string ToString()
        {
            var negative = IsNegative ? "-" : "+";
            var trait = string.IsNullOrEmpty(Trait) ? "" : $"({Trait})";
            var valueTrait = string.IsNullOrEmpty(ValueTrait) ? "" : $"<{ValueTrait}>";
            return $"{negative}[{Target}]{trait}{valueTrait}{ValueString}";
        }

        /// <summary>
        /// 测试里为了便利，有时会用到StateView与State的比较
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool Equals(State state)
        {
            return Target.Equals(state.Target) &&
                   (state.Trait == default
                       ? string.IsNullOrEmpty(Trait)
                       : Trait.Equals(state.Trait.ToString())) &&
                   ValueString.Equals(state.ValueString.ToString()) &&
                   (state.ValueTrait == default
                       ? string.IsNullOrEmpty(ValueTrait)
                       : ValueTrait.Equals(state.ValueTrait.ToString())) &&
                   IsNegative == state.IsNegative;
        }
    }
}