using System;
using Unity.Entities;

namespace DOTS
{
    public struct State : IBufferElementData
    {
        public StateSubjectType SubjectType;
        public ComponentType Trait;
        public NativeString64 Value;
        public bool IsPositive;
        
        public Entity Target;
        
        /// <summary>
        /// Fit的概念表示两个State从要达成的目的角度一致，并不比较具体Entity Target
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Fit(State other)
        {
            return SubjectType == other.SubjectType &&
                   Trait == other.Trait &&
                   Value.Equals(other.Value) &&
                   IsPositive == other.IsPositive;
        }
    }
}