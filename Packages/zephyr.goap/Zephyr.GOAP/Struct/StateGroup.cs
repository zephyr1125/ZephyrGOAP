using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Lib;

namespace Zephyr.GOAP.Struct
{
    public struct StateGroup : IDisposable, IEnumerable<State>, IEquatable<StateGroup>
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
        
        public StateGroup(ZephyrNativeMinHeap<State> copyFrom, Allocator allocator)
        {
            _states = new NativeList<State>(allocator);
            while (copyFrom.HasNext())
            {
                var state = copyFrom.PopMin().Content;
                _states.Add(state);
            }
        }

        public StateGroup(int initialCapacity, NativeMultiHashMap<int, State>.Enumerator copyFrom,
            Allocator allocator)
        {
            _states = new NativeList<State>(initialCapacity, allocator);
            while (copyFrom.MoveNext())
            {
                _states.Add(copyFrom.Current);
            }
        }
        
        /// <summary>
        /// 只拷贝来源的前几个state
        /// </summary>
        /// <param name="copyFrom"></param>
        /// <param name="length"></param>
        /// <param name="allocator"></param>
        public StateGroup(StateGroup copyFrom, int length, Allocator allocator)
        {
            _states = new NativeList<State>(length, allocator);
            for (var i = 0; i < length; i++)
            {
                var state = copyFrom._states[i];
                _states.Add(state);
            }
        }
        
        public StateGroup(NativeMinHeap<State> copyFrom, int length, Allocator allocator)
        {
            _states = new NativeList<State>(length, allocator);
            for (var i = 0; i < length; i++)
            {
                var found = copyFrom.Pop();
                var state = copyFrom[found].Content;
                _states.Add(state);
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
        /// 表示对左侧期望的满足计算，因此结构只有1种可能：
        /// 期望And实现
        /// 会移除掉左侧满足的State，可数的减数量，不可数的移除
        /// </summary>
        /// <param name="other"></param>
        /// <param name="removeOther">是否移除右侧满足的State，不可数不会被移除</param>
        public void AND(StateGroup other, bool removeOther = false)
        {
            for (var thisId = Length() - 1; thisId >= 0; thisId--)
            {
                var thisState = this[thisId];

                for (var otherId = other.Length() - 1; otherId >= 0; otherId--)
                {
                    var otherState = other[otherId];

                    if (thisState.IsCountable())
                    {
                        var thisStateRemoved = false;
                        if (!thisState.SameTo(otherState) && !otherState.BelongTo(thisState))
                            continue;
                        var costAmount = thisState.Amount;
                        if (otherState.Amount>=thisState.Amount)
                        {
                            _states.RemoveAtSwapBack(thisId);
                            thisStateRemoved = true;
                        }
                        else
                        {
                            thisState.Amount -= otherState.Amount;
                            _states[thisId] = thisState;
                        }
                        if (removeOther)
                        {
                            if (costAmount >= otherState.Amount)
                            {
                                other._states.RemoveAtSwapBack(otherId);
                            }
                            else
                            {
                                otherState.Amount -= costAmount;
                                other[otherId] = otherState;
                            }
                        }

                        if (thisStateRemoved) break;
                    }
                    else
                    {
                        //不可数
                        if (!thisState.Equals(otherState) && !otherState.BelongTo(thisState))
                            continue;
                        _states.RemoveAtSwapBack(thisId);
                        break;
                    }
                }
            }
            
        }
        
        /// <summary>
        /// 2种可能：
        /// 期望Or期望，实现Or实现
        /// </summary>
        /// <param name="other"></param>
        public void OR(StateGroup other)
        {
            for (var otherId = 0; otherId < other.Length(); otherId++)
            {
                var otherState = other[otherId];
                var contained = false;

                for (var thisId = 0; thisId < Length(); thisId++)
                {
                    var thisState = this[thisId];

                    if (otherState.IsCountable())
                    {
                        //可数
                        if (!thisState.SameTo(otherState)) continue;
                        
                        //找到了则直接增加数量
                        contained = true;
                        thisState.Amount += otherState.Amount;
                        this[thisId] = thisState;
                        break;
                    }
                    else
                    {
                        //不可数
                        if (!thisState.Equals(otherState)) continue;
                        
                        contained = true;
                        break;
                    }
                }
                //左侧找不到相同项时需要追加
                if(!contained)Add(otherState);
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
            Sub(ref effectStates, out var removedStates, Allocator.Temp);
            removedStates.Dispose();
        }

        /// <summary>
        /// Equal或双向Belong则移除，不同项则无视
        /// </summary>
        /// <param name="other"></param>
        /// <param name="removedStates"></param>
        /// <param name="allocatorForRemovedStates"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public void Sub(ref StateGroup other, out StateGroup removedStates,
            Allocator allocatorForRemovedStates, Func<State, State, State> func = null)
        {
            removedStates = new StateGroup(1, allocatorForRemovedStates);
            
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
                        removedStates.Add(state);
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
            return _states.GetHashCode();
        }

        /// <summary>
        /// 目前即使内容一样，但顺序不一样也不认为equal
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(StateGroup other)
        {
            if (Length() != other.Length()) return false;
            
            for (var i = 0; i < _states.Length; i++)
            {
                if (!_states[i].Equals(other._states[i])) return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is StateGroup other && Equals(other);
        }
    }
}