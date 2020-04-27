using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoapPlanningJob
{
    public struct PathSavingJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<Node> PathResult;

        public EntityCommandBuffer.Concurrent ECBuffer;

        [ReadOnly]
        public NodeGraph NodeGraph;

        public NativeArray<Entity> PathEntities;
        
        public void Execute(int jobId)
        {
            var node = PathResult[jobId];
            var preconditions = NodeGraph.GetNodePreconditions(node, Allocator.Temp);
            var effects = NodeGraph.GetNodeEffects(node, Allocator.Temp);
            
            var entity = ECBuffer.CreateEntity(jobId);
            // add states & dependencies
            var stateBuffer = ECBuffer.AddBuffer<State>(jobId, entity);
            ECBuffer.AddBuffer<NodeDependency>(jobId, entity);
            for (var i = 0; i < preconditions.Length(); i++)
            {
                stateBuffer.Add(preconditions[i]);
                node.PreconditionsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
            }
            for (var i = 0; i < effects.Length(); i++)
            {
                stateBuffer.Add(effects[i]);
                node.EffectsBitmask |= (ulong) 1 << stateBuffer.Length - 1;
            }
            //add node
            ECBuffer.AddComponent(jobId, entity, node);
            
            PathEntities[jobId] = entity;
            
            preconditions.Dispose();
            effects.Dispose();
        }
    }

    /// <summary>
    /// 关联node
    /// </summary>
    public struct PathAddDependencyJob : IJobForEachWithEntity_EBC<NodeDependency, Node>
    {
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> PathEntities;

        [ReadOnly]
        public NativeArray<Node> PathNodes;

        [ReadOnly]
        public BufferFromEntity<State> BufferStates;

        public void Execute(Entity entity, int index, DynamicBuffer<NodeDependency> nodeDependencies, ref Node node)
        {
            //遍历所有节点，如果某个节点的某个effect指向我，那么他是我的一个依赖
            var nodeHash = node.HashCode;
            for (var i = 0; i < PathEntities.Length; i++)
            {
                var otherEntity = PathEntities[i];
                var otherNode = PathNodes[i];
                var otherNodeStates = BufferStates[otherEntity];
                for (var j = 0; j < otherNodeStates.Length; j++)
                {
                    if ((otherNode.EffectsBitmask & (ulong)1<<j) <= 0) continue;
                    var otherEffect = otherNodeStates[j];
                    if (!otherEffect.OwnerNodeHash.Equals(nodeHash)) continue;
                    nodeDependencies.Add(new NodeDependency {Entity = otherEntity});
                }
            }
        }
    }
}