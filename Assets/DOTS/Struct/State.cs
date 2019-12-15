using System;
using Unity.Entities;

namespace DOTS.Struct
{
    public struct State : IBufferElementData, IEquatable<State>
    {
        public StateSubjectType SubjectType;
        public Entity Target;
        public ComponentType Trait;
        public NativeString64 ValueString;
        public ComponentType ValueTrait;
        public bool IsPositive;

        public bool Equals(State other)
        {
            return SubjectType.Equals(other.SubjectType) && Trait.Equals(other.Trait) &&
                   ValueString.Equals(other.ValueString) && IsPositive.Equals(other.IsPositive) &&
                   Target.Equals(other.Target);
        }
        
        public static State Null = new State();

        /// <summary>
        /// 在指定Target方面比Equals宽松
        /// 非指定Entity的Closest与指定Entity之间也算Fit
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Fits(State other)
        {
            if (Equals(other)) return true;

            //这三项必须一致
            if (!(Trait.Equals(other.Trait) && ValueString.Equals(other.ValueString) &&
                  IsPositive.Equals(IsPositive)))
            {
                return false;
            }

            //我与对方任何一个为Closest另一个为指定Entity则算作Fit
            if (SubjectType == StateSubjectType.Closest && other.Target != Entity.Null) return true;
            if (Target != Entity.Null && other.SubjectType == StateSubjectType.Closest) return true;

            return false;
        }

        /// <summary>
        /// 范围的从属关系，意指other是一个包含自己的大范围state，在类型筛选上比Equals宽松
        /// 但要注意从属关系是有方向的，只支持other包含this
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool BelongTo(State other)
        {
            if (Equals(Null) || other.Equals(Null)) return false;
            
            //凡是other不明指的项目，都可以包含this
            if (other.SubjectType!=StateSubjectType.NoSpecific && SubjectType != other.SubjectType) return false;
            if (other.Target!=Entity.Null && Target != other.Target) return false;
            if (other.Trait!=null && Trait != other.Trait) return false;
            if (other.ValueTrait!=null && ValueTrait != other.ValueTrait) return false;
            if (!other.ValueString.Equals(new NativeString64()) && !ValueString.Equals(other.ValueString)) return false;
            
            //positive是必须明指的
            if (IsPositive != other.IsPositive) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + SubjectType.GetHashCode();
            hash = hash * 31 + Target.GetHashCode();
            hash = hash * 31 + Trait.GetHashCode();
            hash = hash * 31 + ValueString.GetHashCode();
            hash = hash * 31 + IsPositive.GetHashCode();
            return hash;
        }
    }
}