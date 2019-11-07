using System;
using Unity.Entities;

namespace DOTS
{
    public struct State : IBufferElementData, IEquatable<State>
    {
        public Entity Target;
        public ComponentType Trait;
        public NativeString64 StringValue;

        public bool Equals(State other)
        {
            return Target == other.Target &&
                   Trait == other.Trait &&
                   StringValue.Equals(other.StringValue);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}