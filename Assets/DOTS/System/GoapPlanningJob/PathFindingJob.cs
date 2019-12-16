using System;
using DOTS.Lib;
using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System.GoapPlanningJob
{
    [BurstCompile]
    public struct PathFindingJob : IJob
    {
        public int StartNodeId, GoalNodeId;
            
        [ReadOnly]
        public NodeGraph NodeGraph;

        public int IterationLimit;

        public int PathNodeLimit;

        private int _iterations, _pathNodeCount;

        [NativeDisableParallelForRestriction]
        public NativeList<Node> Result;
            
        public void Execute()
        {
            _iterations = 0;
            _pathNodeCount = 0;

            var graphSize = NodeGraph.Length();
                
            //Generate Working Containers
            var openSet = new NativeMinHeap(graphSize, Allocator.Temp);
            var cameFrom = new NativeHashMap<int, int>(graphSize, Allocator.Temp);
            var costCount = new NativeHashMap<int, int>(graphSize, Allocator.Temp);

            // Path finding
            var startId = StartNodeId;
            var goalId = GoalNodeId;
                
            openSet.Push(new MinHeapNode(startId, 0));
            costCount[startId] = 0;

            var currentId = -1;
            while (_iterations<IterationLimit && openSet.HasNext())
            {
                var currentNode = openSet[openSet.Pop()];
                currentId = currentNode.Id;
                if (currentId == goalId)
                {
                    break;
                }
                    
                var neighboursId = new NativeList<int>(4, Allocator.Temp);
                NodeGraph[currentId].GetNeighbours(ref NodeGraph, ref neighboursId);

                foreach (var neighbourId in neighboursId)
                {
                    //if cost == -1 means obstacle, skip
                    if (NodeGraph[neighbourId].GetCost(ref NodeGraph) == -1) continue;

                    var currentCost = costCount[currentId] == Int32.MaxValue
                        ? 0
                        : costCount[currentId];
                    var newCost = currentCost + NodeGraph[neighbourId].GetCost(ref NodeGraph);
                    //not better, skip
                    if (costCount.ContainsKey(neighbourId) && costCount[neighbourId] <= newCost) continue;
                        
                    var priority = newCost + NodeGraph[neighbourId].Heuristic(ref NodeGraph);
                    openSet.Push(new MinHeapNode(neighbourId, priority));
                    cameFrom[neighbourId] = currentId;
                    costCount[neighbourId] = newCost;
                }

                _iterations++;
                neighboursId.Dispose();
            }
                
            //Construct path
            var nodeId = goalId;
            while (_pathNodeCount < PathNodeLimit && !nodeId.Equals(startId))
            {
                Result.Add(NodeGraph[nodeId]);
                nodeId = cameFrom[nodeId];
                _pathNodeCount++;
            }
                
            //Log Result
            var success = true;
            var log = new NativeString64("Path finding success");
            if (!openSet.HasNext() && currentId != goalId)
            {
                success = false;
                log = new NativeString64("Out of openset");
            }
            if (_iterations >= IterationLimit && currentId != goalId)
            {
                success = false;
                log = new NativeString64("Iteration limit reached");
            }else if (_pathNodeCount >= PathNodeLimit && !nodeId.Equals(startId))
            {
                success = false;
                log = new NativeString64("Step limit reached");
            }
                
            //Clear
            openSet.Dispose();
            cameFrom.Dispose();
            costCount.Dispose();
        }
            
    }
}