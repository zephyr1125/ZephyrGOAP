using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Lib;

namespace Zephyr.GOAP.System.GoapPlanningJob
{
    public struct PrepareNodeAgentPairsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Entity> Entities;
        
        [ReadOnly]
        public NativeList<Node> Nodes;
        
        public NativeArray<ZephyrValueTuple<Entity, Node>> NodeAgentPairs;
        
        public void Execute(int pairId)
        {
            var nodeAmount = Nodes.Length;
            var entityId = pairId / nodeAmount;
            var nodeId = pairId % nodeAmount;
            
            NodeAgentPairs[pairId] = new ZephyrValueTuple<Entity, Node>(Entities[entityId], Nodes[nodeId]);
        }
    }
}