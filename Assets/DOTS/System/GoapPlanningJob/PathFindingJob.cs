using System;
using DOTS.Lib;
using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System.GoapPlanningJob
{
    // [BurstCompile]
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
            var rewardSum = new NativeHashMap<int, float>(graphSize, Allocator.Temp);

            // Path finding
            var startId = StartNodeId;
            var goalId = GoalNodeId;
                
            openSet.Push(new MinHeapNode(startId, 0));
            rewardSum[startId] = 0;

            var currentId = -1;
            while (_iterations<IterationLimit && openSet.HasNext())
            {
                var currentNode = openSet[openSet.Pop()];
                currentId = currentNode.Id;
                
                //不使用early quit，因为会使用非最优解
                //early quit
                // if (currentId == goalId)
                // {
                //     break;
                // }
                    
                var neighboursId = new NativeList<int>(4, Allocator.Temp);
                NodeGraph[currentId].GetNeighbours(ref NodeGraph, ref neighboursId);

                foreach (var neighbourId in neighboursId)
                {
                    //if reward == -infinity means obstacle, skip
                    if (float.IsNegativeInfinity(NodeGraph[neighbourId].GetReward(ref NodeGraph))) continue;
                    
                    var newReward = rewardSum[currentId] + NodeGraph[neighbourId].GetReward(ref NodeGraph);
                    //not better, skip
                    if (rewardSum.ContainsKey(neighbourId) && rewardSum[neighbourId] >= newReward) continue;
                        
                    var priority = -(newReward + NodeGraph[neighbourId].Heuristic(ref NodeGraph));
                    openSet.Push(new MinHeapNode(neighbourId, priority));
                    cameFrom[neighbourId] = currentId;
                    rewardSum[neighbourId] = newReward;
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
            rewardSum.Dispose();
        }
            
    }
}