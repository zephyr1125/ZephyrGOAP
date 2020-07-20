using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Zephyr.GOAP.Lib
{
    /// <summary>
    /// 由于第三方NativeMinHeap不能进行指定删除
    /// 所以我基于NativeList包装了一个伪MinHeap
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [NativeContainer]
    public struct ZephyrNativeMinHeap<T> : IDisposable where T : struct,IEquatable<T>
    {
        private NativeList<MinHashNode<T>> _nodes;

        public ZephyrNativeMinHeap(Allocator allocator)
        {
            _nodes = new NativeList<MinHashNode<T>>(allocator);
        }

        /// <summary>
        /// 如果有相同Content的Node存在，自动移除旧的
        /// </summary>
        /// <param name="node"></param>
        public void Add(MinHashNode<T> node)
        {
            for (var i = 0; i < _nodes.Length; i++)
            {
                if (!_nodes[i].Content.Equals(node.Content)) continue;
                _nodes.RemoveAtSwapBack(i);
                break;
            }
            _nodes.Add(node);
        }

        public MinHashNode<T> PopMin()
        {
            var minNode = new MinHashNode<T>(default, float.MaxValue);
            var minIndex = -1;
            for (var i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];
                if (node.Priority >= minNode.Priority) continue;
                minNode = node;
                minIndex = i;
            }
            _nodes.RemoveAtSwapBack(minIndex);
            return minNode;
        }

        public bool HasNext()
        {
            return _nodes.Length > 0;
        }
        
        public void Dispose()
        {
            _nodes.Dispose();
        }
    }
    
    public readonly struct MinHashNode<T> where T:struct
    {
        public MinHashNode(T content, float priority)
        {
            Content = content;
            Priority = priority;
        }
 
        public T Content { get; } // TODO to position
        public float Priority { get; }
    }
}