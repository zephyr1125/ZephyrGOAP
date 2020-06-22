using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Zephyr.GOAP.Tests.TestAndLearn.ParallelWriter
{
    public class TestParallelWriter : TestBase
    {
        private ParallelWriterTestSystem _system;

        public override void SetUp()
        {
            base.SetUp();
            _system = World.GetOrCreateSystem<ParallelWriterTestSystem>();

        }

        [Test]
        public void TestRun()
        {
            // _system.Update();
            // EntityManager.CompleteAllJobs();
            
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    _system.Update();
                    EntityManager.CompleteAllJobs();
                }, "不可以两个Job同时操作一个ParallelWriter");
        }
    }
}