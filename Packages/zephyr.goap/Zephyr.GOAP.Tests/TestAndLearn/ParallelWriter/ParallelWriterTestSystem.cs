using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Zephyr.GOAP.Tests.TestAndLearn.ParallelWriter
{
    [DisableAutoCreation]
    public class ParallelWriterTestSystem : SystemBase
    {
        public NativeQueue<int> Container;

        protected override void OnCreate()
        {
            base.OnCreate();
            Container = new NativeQueue<int>(Allocator.Persistent);
        }

        private struct ParallelWriteJob: IJobParallelFor
        {
            public NativeQueue<int>.ParallelWriter Writer;
            
            public void Execute(int index)
            {
                Writer.Enqueue(index);
            }
        }
        
        private struct ParallelWriteJobB: IJobParallelFor
        {
            public NativeQueue<int>.ParallelWriter Writer;
            
            public void Execute(int index)
            {
                Writer.Enqueue(-index);
            }
        }

        protected override void OnUpdate()
        {
            var dependency = Dependency;
            var writer = Container.AsParallelWriter();
            var job = new ParallelWriteJob
            {
                Writer = writer
            };
            var jobB = new ParallelWriteJobB
            {
                Writer = Container.AsParallelWriter()
            };
            
            var handle = job.Schedule(16, 4, dependency);
            Dependency = handle;
            
            var handleB = jobB.Schedule(16, 4, dependency);
            Dependency = JobHandle.CombineDependencies(handle, handleB);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Container.Dispose(Dependency);
        }
    }
}