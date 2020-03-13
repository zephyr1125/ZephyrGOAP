using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Zephyr.GOAP.Struct
{
    public struct State : IBufferElementData, IEquatable<State>
    {
        public Entity Target;
        public float3 Position;
        public ComponentType Trait;
        public NativeString64 ValueString;
        public ComponentType ValueTrait;
        /// <summary>
        /// true时表示这个state表达反面意义，比如“目标不拥有指定物品”
        /// </summary>
        public bool IsNegative;
        
        public bool Equals(State other)
        {
            return Trait.Equals(other.Trait) &&
                   ValueString.Equals(other.ValueString) && IsNegative.Equals(other.IsNegative) &&
                   Target.Equals(other.Target);
        }
        
        public static State Null = new State();

        /// <summary>
        /// 范围的从属关系，意指other是一个包含自己的大范围state，类型筛选上比Equals宽松
        /// 但要注意从属关系是有方向的，只支持other包含this
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool BelongTo(State other)
        {
            if (Equals(Null) || other.Equals(Null)) return false;
            
            //凡是other不明指的项目，都可以包含this
            if (other.Target!=Entity.Null && Target != other.Target) return false;
            if (other.Trait!=null && Trait != other.Trait) return false;
            if (ValueTrait!=null && other.ValueTrait!=null && ValueTrait != other.ValueTrait) return false;
            if (!other.ValueString.Equals(new NativeString64()) && !ValueString.Equals(other.ValueString)) return false;
            
            if (IsNegative != other.IsNegative) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + Target.GetHashCode();
            hash = hash * 31 + Trait.GetHashCode();
            hash = hash * 31 + ValueString.GetHashCode();
            hash = hash * 31 + IsNegative.GetHashCode();
            return hash;
        }

        /// <summary>
        /// 是否为范围型state
        /// </summary>
        /// <returns></returns>
        public bool IsScopeState()
        {
            return ValueString.Equals(new NativeString64()) || Target.Equals(Entity.Null);
        }
    }
}