using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Zephyr.GOAP.Test.TestAndLearn.ParallelWriter
{
    [DisableAutoCreation]
    public class ParallelWriterTestSystem : JobComponentSystem
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

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new ParallelWriteJob
            {
                Writer = Container.AsParallelWriter()
            };
            var jobB = new ParallelWriteJob
            {
                Writer = Container.AsParallelWriter()
            };
            var handle = job.Schedule(16, 4, inputDeps);
            var handleB = jobB.Schedule(16, 4, inputDeps);
            return JobHandle.CombineDependencies(handle, handleB);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Container.Dispose();
        }
    }
}