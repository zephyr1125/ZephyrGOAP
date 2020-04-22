using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Zephyr.GOAP.Struct;
using Unity.Mathematics;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class StateLog : IComparable<StateLog>
    {
        public EntityLog Target;
        public float3 Position;
        public string Trait;
        public string ValueString;
        public string ValueTrait;
        public bool IsNegative;

        public StateLog(EntityManager entityManager, State state)
        {
            Target = new EntityLog(entityManager, state.Target);
            Position = state.Position;
            if(state.Trait!=default)Trait = state.Trait.ToString();
            ValueString = state.ValueString.ToString();
            if(state.ValueTrait!=default)ValueTrait = state.ValueTrait.ToString();
            IsNegative = state.IsNegative;
        }

        public static StateLog[] CreateStateLogs(EntityManager entityManager, State[] states)
        {
            var stateLogs = new List<StateLog>(states.Length);
            stateLogs.AddRange(states.Select(t => new StateLog(entityManager, t)));
            return stateLogs.ToArray();
        }

        public int CompareTo(StateLog other)
        {
            return Target.CompareTo(other.Target)
                + Trait.CompareTo(other.Trait)
                + ValueString.CompareTo(other.ValueString)
                + ValueTrait.CompareTo(other.ValueTrait)
                + IsNegative.CompareTo(other.IsNegative);
        }

        public override string ToString()
        {
            var negative = IsNegative ? "-" : "+";
            var trait = string.IsNullOrEmpty(Trait) ? "" : $"({Trait})";
            var valueTrait = string.IsNullOrEmpty(ValueTrait) ? "" : $"<{ValueTrait}>";
            var position = Target.Equals(Entity.Null)
                ? ""
                : $"({Position.x},{Position.y},{Position.z})";
            return $"{negative}[{Target}]{trait}{valueTrait}{ValueString}{position}";
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