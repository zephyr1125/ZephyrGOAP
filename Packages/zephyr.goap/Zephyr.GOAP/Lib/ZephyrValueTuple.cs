using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zephyr.GOAP.Lib
{
    [Serializable]
    public struct ZephyrValueTuple<T1, T2>
        : IStructuralComparable,
            IStructuralEquatable,
            IComparable,
            IComparable<ZephyrValueTuple<T1, T2>>,
            IEquatable<ZephyrValueTuple<T1, T2>>, ITuple
    {
        public T1 Item1;
        public T2 Item2;
     
        public ZephyrValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
     
        public int CompareTo(ZephyrValueTuple<T1, T2> other)
        {
            if (Comparer<T1>.Default.Compare(Item1, other.Item1) != 0)
            {
                return Comparer<T1>.Default.Compare(Item1, other.Item1);
            }
     
            return Comparer<T2>.Default.Compare(Item2, other.Item2);
        }
     
        public override bool Equals(object obj)
        {
            if (!(obj is ZephyrValueTuple<T1, T2>))
            {
                return false;
            }
     
            ZephyrValueTuple<T1, T2> other = (ZephyrValueTuple<T1, T2>) obj;
            return EqualityComparer<ZephyrValueTuple<T1, T2>>.Equals(
                       Item1,
                       other.Item1)
                   && EqualityComparer<ZephyrValueTuple<T1, T2>>.Equals(
                       Item2,
                       other.Item2);
        }
     
        public bool Equals(ZephyrValueTuple<T1, T2> other)
        {
            return EqualityComparer<ZephyrValueTuple<T1, T2>>.Equals(
                       Item1,
                       other.Item1)
                   && EqualityComparer<ZephyrValueTuple<T1, T2>>.Equals(
                       Item2,
                       other.Item2);
        }
     
        private static readonly int randomSeed = new Random().Next(
            int.MinValue,
            int.MaxValue);
     
        private static int Combine(int h1, int h2)
        {
            // RyuJIT optimizes this to use the ROL instruction
            // Related GitHub pull request: dotnet/coreclr#1830
            uint rol5 = ((uint) h1 << 5) | ((uint) h1 >> 27);
            return ((int) rol5 + h1) ^ h2;
        }
     
        private static int CombineHashCodes(int h1, int h2)
        {
            return Combine(Combine(randomSeed, h1), h2);
        }
     
        private static int CombineHashCodes(int h1, int h2, int h3)
        {
            return Combine(CombineHashCodes(h1, h2), h3);
        }
     
        public override int GetHashCode()
        {
            return CombineHashCodes(
                Item1?.GetHashCode() ?? 0,
                Item2?.GetHashCode() ?? 0);
        }
     
        int IStructuralComparable.CompareTo(object obj, IComparer comparer)
        {
            if (obj == null)
            {
                return 1;
            }
     
            if (!(obj is ZephyrValueTuple<T1, T2>))
            {
                throw new ArgumentException("Incorrect type", "obj");
            }
     
            ZephyrValueTuple<T1, T2> other = (ZephyrValueTuple<T1, T2>) obj;
     
            if (Comparer<T1>.Default.Compare(Item1, other.Item1) != 0)
            {
                return Comparer<T1>.Default.Compare(Item1, other.Item1);
            }
     
            return Comparer<T2>.Default.Compare(Item2, other.Item2);
        }
     
        bool IStructuralEquatable.Equals(object obj, IEqualityComparer comparer)
        {
            if (!(obj is ZephyrValueTuple<T1, T2>))
            {
                return false;
            }
     
            ZephyrValueTuple<T1, T2> other = (ZephyrValueTuple<T1, T2>) obj;
            return comparer.Equals(
                       Item1,
                       other.Item1)
                   && comparer.Equals(
                       Item2,
                       other.Item2);
        }
     
        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            return CombineHashCodes(
                Item1?.GetHashCode() ?? 0,
                Item2?.GetHashCode() ?? 0);
        }
     
        int IComparable.CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }
     
            if (!(other is ZephyrValueTuple<T1, T2>))
            {
                throw new ArgumentException("Incorrect type", "other");
            }
     
            return CompareTo((ZephyrValueTuple<T1, T2>) other);
        }
     
        public override string ToString()
        {
            return $"({Item1}, {Item2})";
        }

        public void Deconstruct(out T1 item1, out T2 item2)
        {
            item1 = Item1;
            item2 = Item2;
        }

        public object this[int index]
        {
            get
            {
                if (index == 0)
                    return Item1;
                if (index == 1)
                    return Item2;
                throw new IndexOutOfRangeException();
            }
        }

        public int Length => 2;
    }
}