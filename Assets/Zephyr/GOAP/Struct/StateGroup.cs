using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Struct
{
    public struct StateGroup : IDisposable, IEnumerable<State>
    {
        [NativeDisableParallelForRestriction]
        private NativeList<State> _states;

        public StateGroup(int initialCapacity, Allocator allocator)
        {
            _states = new NativeList<State>(
                initialCapacity, allocator);
        }

        public StateGroup(StateGroup copyFrom, Allocator allocator)
        {
            _states = new NativeList<State>(copyFrom.Length(), allocator);
            for (var i = 0; i < copyFrom._states.Length; i++)
            {
                var state = copyFrom._states[i];
                _states.Add(state);
            }
        }

        public StateGroup(int length, NativeMultiHashMap<Node, State>.Enumerator copyFrom,
            Allocator allocator)
        {
            _states = new NativeList<State>(length, allocator);
            while (copyFrom.MoveNext())
            {
                _states.Add(copyFrom.Current);
            }
        }
        
        public StateGroup(ref DynamicBuffer<State> statesBuffer,
            Allocator allocator)
        {
            _states = new NativeList<State>(statesBuffer.Length, allocator);
            foreach (var state in statesBuffer)
            {
                _states.Add(state);
            }
        }

        public StateGroup(State[] states, Allocator allocator)
        {
            _states = new NativeList<State>(states.Length, allocator);
            foreach (var state in states)
            {
                _states.Add(state);
            }
        }

        public State this[int key]
        {
            get => _states[key];
            set => _states[key] = value;
        }

        public int Length()
        {
            return _states.Length;
        }

        public void Dispose()
        {
            _states.Dispose();
        }

        public void Add(State state)
        {
            _states.Add(state);
        }

        /// <summary>
        /// Equal或双向Belong则无视，不同项则增加
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public void Merge(StateGroup other)
        {
            //todo 还需要考虑冲突可能，即针对同一个目标的两个state不相容
            for (var i = 0; i < other._states.Length; i++)
            {
                var otherState = other._states[i];
                var contained = false;
                for (var j = 0; j < _states.Length; j++)
                {
                    var state = _states[j];
                    if (state.Equals(otherState)
                        || state.BelongTo(otherState) || otherState.BelongTo(state))
                    {
                        contained = true;
                        break;
                    }
                }

                if (!contained) _states.Add(otherState);
            }
        }

        /// <summary>
        /// Equal或双向Belong则移除，不同项则无视
        /// 如果出现移除，effect需要记录被自己移除的state信息
        /// </summary>
        /// <param name="effectStates"></param>
        /// <returns></returns>
        public void SubForEffect(ref StateGroup effectStates)
        {
            Sub(ref effectStates, (State state, State effect) =>
                {
                    var preconditionHash = state.GetHashCode();
                    return effect;
                }
            );
        }
        
        /// <summary>
        /// Equal或双向Belong则移除，不同项则无视
        /// </summary>
        /// <param name="other"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public void Sub(ref StateGroup other, Func<State, State, State> func = null)
        {
            for (var i = 0; i < other._states.Length; i++)
            {
                var otherState = other._states[i];
                for (var j = _states.Length - 1; j >= 0; j--)
                {
                    var state = _states[j];
                    if (state.Equals(otherState)
                        || state.BelongTo(otherState) || otherState.BelongTo(state))
                    {
                        if (func != null)
                        {
                            otherState = func(state, otherState);
                            other[i] = otherState;
                        }
                        _states.RemoveAtSwapBack(j);
                    }
                }
            }
        }

        public State GetState(Func<State, bool> compare)
        {
            foreach (var state in _states)
            {
                if (compare(state)) return state;
            }
            return State.Null;
        }

        public State GetBelongingState(State belongTo)
        {
            foreach (var state in _states)
            {
                if (state.BelongTo(belongTo)) return state;
            }
            return State.Null;
        }

        public StateGroup GetBelongingStates(State belongTo, Allocator allocator)
        {
            var group = new StateGroup(3, allocator);
            foreach (var state in _states)
            {
                if (state.BelongTo(belongTo)) group.Add(state);
            }

            return group;
        }

        public void WriteBuffer(ref DynamicBuffer<State> buffer)
        {
            foreach (var state in _states)
            {
                buffer.Add(state);
            }
        }

        public IEnumerator<State> GetEnumerator()
        {
            return _states.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override int GetHashCode()
        {
            var sum = 0;
            if (_states.IsCreated)
            {
                for (var i = 0; i < _states.Length; i++)
                {
                    var state = _states[i];
                    sum += state.GetHashCode();
                }
            }

            return sum;
        }
    }
}