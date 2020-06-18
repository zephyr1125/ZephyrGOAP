using System;
using NUnit.Framework;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Zephyr.GOAP.Tests.TestAndLearn
{
    public class TestRandom
    {
        private Random _random;

        [Test]
        public void DifferentRandomResults()
        {
            Debug.Log(DateTime.Now.Millisecond);
            _random = new Random((uint)DateTime.Now.Millisecond);
            for (var i = 0; i < 10; i++)
            {
                var result = _random.NextFloat2Direction();
                Debug.Log(result);
            }
        }

        /// <summary>
        /// 初始化random后的首个结果是基于seed值大小的顺序序列中的一个
        /// </summary>
        [Test]
        public void SortedSequenceAtFirst()
        {
            for (var i = 1; i < 100; i++)
            {
                Debug.Log(i);
                _random = new Random((uint)i);
                var result = _random.NextFloat2Direction();
                Debug.Log(result);
            }
        }
    }
}