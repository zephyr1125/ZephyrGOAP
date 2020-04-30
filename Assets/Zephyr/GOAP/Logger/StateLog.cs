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
        public EntityLog target;
        public float3 position;
        public string trait;
        public string valueString;
        public string valueTrait;
        public bool isNegative;

        public StateLog(EntityManager entityManager, State state)
        {
            target = new EntityLog(entityManager, state.Target);
            position = state.Position;
            if(state.Trait!=default)trait = state.Trait.ToString();
            valueString = state.ValueString.ToString();
            if(state.ValueTrait!=default)valueTrait = state.ValueTrait.ToString();
            isNegative = state.IsNegative;
        }

        public static StateLog[] CreateStateLogs(EntityManager entityManager, State[] states)
        {
            var stateLogs = new List<StateLog>(states.Length);
            stateLogs.AddRange(states.Select(t => new StateLog(entityManager, t)));
            return stateLogs.ToArray();
        }

        public int CompareTo(StateLog other)
        {
            return target.CompareTo(other.target)
                + trait.CompareTo(other.trait)
                + valueString.CompareTo(other.valueString)
                + valueTrait.CompareTo(other.valueTrait)
                + isNegative.CompareTo(other.isNegative);
        }

        public override string ToString()
        {
            var negative = isNegative ? "-" : "+";
            var trait = string.IsNullOrEmpty(this.trait) ? "" : $"({this.trait})";
            var valueTrait = string.IsNullOrEmpty(this.valueTrait) ? "" : $"<{this.valueTrait}>";
            var position = target.Equals(Entity.Null)
                ? ""
                : $"({this.position.x},{this.position.y},{this.position.z})";

            return $"{negative}[{target}]{trait}{valueTrait}{valueString}{position}";
        }

        /// <summary>
        /// 测试里为了便利，有时会用到StateView与State的比较
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool Equals(State state)
        {
            return target.Equals(state.Target) &&
                   (state.Trait == default
                       ? string.IsNullOrEmpty(trait)
                       : trait.Equals(state.Trait.ToString())) &&
                   valueString.Equals(state.ValueString.ToString()) &&
                   (state.ValueTrait == default
                       ? string.IsNullOrEmpty(valueTrait)
                       : valueTrait.Equals(state.ValueTrait.ToString())) &&
                   isNegative == state.IsNegative;
        }
    }
}