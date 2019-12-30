using System;
using System.Collections.Generic;
using System.Linq;
using DOTS.Struct;
using Unity.Entities;

namespace DOTS.Logger
{
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
            if(state.Trait!=null)Trait = state.Trait.ToString();
            ValueString = state.ValueString.ToString();
            if(state.ValueTrait!=null)ValueTrait = state.ValueTrait.ToString();
            IsNegative = state.IsNegative;
        }

        public static StateView[] CreateStateViews(EntityManager entityManager, State[] states)
        {
            var stateViews = new List<StateView>(states.Length);
            stateViews.AddRange(states.Select(t => new StateView(entityManager, t)));
            return stateViews.ToArray();
        }
    }
}