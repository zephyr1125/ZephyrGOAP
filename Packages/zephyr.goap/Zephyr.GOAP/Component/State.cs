using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Zephyr.GOAP.Component
{
    public struct State : IBufferElementData, IEquatable<State>
    {
        public Entity Target;
        public float3 Position;
        public int Trait;
        public FixedString32 ValueString;
        public int ValueTrait;
        public int Amount;
        /// <summary>
        /// true时表示这个state表达反面意义，比如“目标不拥有指定物品”
        /// </summary>
        public bool IsNegative;

        public static State Null => new State();
        
        public bool Equals(State other)
        {
            return SameTo(other) && Amount.Equals(other.Amount);
        }

        /// <summary>
        /// 除了数量，其他都equal,算SameTo
        /// 但是一个不可数(Amount==0)和一个可数(Amount>0)是不能Same的
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool SameTo(State other)
        {
            if (!IsCountable().Equals(other.IsCountable())) return false;
            
            return Trait.Equals(other.Trait) && ValueTrait.Equals(other.ValueTrait) &&
                   ValueString.Equals(other.ValueString) && IsNegative.Equals(other.IsNegative) &&
                   Target.Equals(other.Target);
        }

        public bool IsCountable()
        {
            return Amount > 0;
        }

        /// <summary>
        /// 范围的从属关系，意指other是一个包含自己的大范围state，类型筛选上比Equals宽松
        /// 但要注意从属关系是有方向的，只支持other包含this
        /// equal也算belong to
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool BelongTo(State other)
        {
            if (Equals(default) || other.Equals(default)) return false;

            if (Equals(other)) return true;
            
            //凡是other不明指的项目，都可以包含this
            if (other.Target!=Entity.Null && Target != other.Target) return false;
            if (other.Trait!=default && Trait != other.Trait) return false;
            if (ValueTrait!=default && other.ValueTrait!=default && ValueTrait != other.ValueTrait) return false;
            if (!other.ValueString.Equals(new FixedString32()) && !ValueString.Equals(other.ValueString)) return false;
            
            if (IsNegative != other.IsNegative) return false;

            return true;
        }

        public override int GetHashCode()
        {
            var hash = Utils.BasicHash;
            hash = Utils.CombineHash(hash, Utils.GetEntityHash(Target));
            hash = Utils.CombineHash(hash, Trait.GetHashCode());
            hash = Utils.CombineHash(hash, ValueString.GetHashCode());
            hash = Utils.CombineHash(hash, ValueTrait.GetHashCode());
            hash = Utils.CombineHash(hash, Amount.GetHashCode());
            hash = Utils.CombineHash(hash, IsNegative.GetHashCode());
            return hash;
        }

        /// <summary>
        /// 是否为范围型state
        /// </summary>
        /// <returns></returns>
        public bool IsScopeState()
        {
            return ValueString.Equals(new FixedString32()) || Target.Equals(Entity.Null);
        }
    }
}