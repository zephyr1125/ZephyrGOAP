using NUnit.Framework;
using UnityEngine;

namespace Zephyr.GOAP.Test.TestAndLearn.ParallelWriter
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
            _system.Update();
            EntityManager.CompleteAllJobs();

            while (_system.Container.Count>0)
            {
                Debug.Log(_system.Container.Dequeue());
            }
           
        }
    }
}