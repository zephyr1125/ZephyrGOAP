using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class StateView
    {
        public EntityView Target;
        public string Trait;
        public string ValueString;
        public string ValueTrait;
        public bool IsNegative;

        public StateView(EntityManager entityManager, State state)
        {
            Target = new EntityView(entityManager, state.Target);
            if(state.Trait!=default)Trait = state.Trait.ToString();
            ValueString = state.ValueString.ToString();
            if(state.ValueTrait!=default)ValueTrait = state.ValueTrait.ToString();
            IsNegative = state.IsNegative;
        }

        public static StateView[] CreateStateViews(EntityManager entityManager, State[] states)
        {
            var stateViews = new List<StateView>(states.Length);
            stateViews.AddRange(states.Select(t => new StateView(entityManager, t)));
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