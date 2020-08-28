using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Struct;
using Unity.Mathematics;
using Zephyr.GOAP.Component;

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
        public int amount;

        public StateLog(EntityManager entityManager, State state)
        {
            target = new EntityLog(entityManager, state.Target);
            position = state.Position;
            if(state.Trait!=default)trait = TypeManager.GetType(state.Trait).Name;
            valueString = state.ValueString.ToString();
            if(state.ValueTrait!=default)valueTrait = TypeManager.GetType(state.ValueTrait).Name;
            isNegative = state.IsNegative;
            amount = state.Amount;
        }

        public static StateLog[] CreateStateLogs(EntityManager entityManager, State[] states)
        {
            var stateLogs = new List<StateLog>(states.Length);
            stateLogs.AddRange(states.Select(t => new StateLog(entityManager, t)));
            return stateLogs.ToArray();
        }

        public static StateLog[] CreateStateLogs(EntityManager entityManager, 
            int nodeHash, NativeList<int> stateIndices,
            NativeList<State> states)
        {
            var stateLogs = new List<StateLog>();
            for (var i = 0; i < stateIndices.Length; i++)
            {
                if (!stateIndices[i].Equals(nodeHash)) continue;
                stateLogs.Add(new StateLog(entityManager, states[i]));
            }

            return stateLogs.ToArray();
        }

        public int CompareTo(StateLog other)
        {
            var targetCompare = 0;
            if (target != null && other.target != null)
            {
                targetCompare = target.CompareTo(other.target);
            }

            var valueTraitCompare = 0;
            if (valueTrait != null && other.valueTrait != null)
            {
                valueTraitCompare = valueTrait.CompareTo(other.valueTrait);
            }
            
            return targetCompare
                   + trait.CompareTo(other.trait)
                   + valueString.CompareTo(other.valueString)
                   + valueTraitCompare
                   + isNegative.CompareTo(other.isNegative);
        }

        public override string ToString()
        {
            var negative = isNegative ? "-" : "+";
            var traitText = string.IsNullOrEmpty(trait) ? "" : $"({trait})";
            var valueTraitText = string.IsNullOrEmpty(valueTrait) ? "" : $"<{valueTrait}>";
            var positionText = target.Equals(Entity.Null)
                ? ""
                : $"({position.x},{position.y},{position.z})";
            var amountText = amount == 0 ? "" : $"*{amount}";

            return $"{negative}[{target}]{traitText}{valueTraitText}{valueString}{positionText}{amountText}";
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
                       : trait.Equals(TypeManager.GetType(state.Trait).Name)) &&
                   valueString.Equals(state.ValueString.ToString()) &&
                   (state.ValueTrait == default
                       ? string.IsNullOrEmpty(valueTrait)
                       : valueTrait.Equals(TypeManager.GetType(state.ValueTrait).Name)) &&
                   isNegative == state.IsNegative;
        }
    }
}