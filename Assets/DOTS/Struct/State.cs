using System;
using Unity.Entities;

namespace DOTS.Struct
{
    public struct State : IBufferElementData, IEquatable<State>
    {
        public StateSubjectType SubjectType;
        public ComponentType Trait;
        public NativeString64 Value;
        public bool IsPositive;
        
        public Entity Target;

        public override int GetHashCode()
        {
            return Target.GetHashCode() + SubjectType.GetHashCode() + Trait.GetHashCode() +
                   Value.GetHashCode() + IsPositive.GetHashCode();
        }

        public bool Equals(State other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is State other && Equals(other);
        }
    }
}