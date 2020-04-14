using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoapPlanningJob
{
    // [BurstCompile]
    public struct PathFindingJob : IJob
    {
        public int StartNodeId, GoalNodeId;
            
        [ReadOnly]
        public NodeGraph NodeGraph;

        [ReadOnly]
        public NativeArray<Entity> AgentEntities;
        [ReadOnly]
        public NativeArray<float3> AgentStartPositions;
        [ReadOnly]
        public NativeArray<float> AgentStartTime;
        [ReadOnly]
        public NativeArray<MaxMoveSpeed> AgentMoveSpeeds;

        public int IterationLimit;

        public int PathNodeLimit;
        
        private int _iterations, _pathNodeCount;

        [NativeDisableParallelForRestriction]
        public NativeList<Node> Result;

        public NativeMultiHashMap<int, NodeTime> TimeResult;
            
        public void Execute()
        {
            _iterations = 0;
            _pathNodeCount = 0;

            var graphSize = NodeGraph.Length();
                
            //Generate Working Containers
            var openSet = new NativeMinHeap<int>(graphSize, Allocator.Temp);
            var cameFrom = new NativeHashMap<int, int>(graphSize, Allocator.Temp);
            var rewardSum = new NativeHashMap<int, float>(graphSize, Allocator.Temp);

            // Path finding
            var startId = StartNodeId;
            var goalId = GoalNodeId;

            openSet.Push(new MinHeapNode<int>(startId, 0));
            
            rewardSum[startId] = 0;
            InitTimeSum(ref TimeResult, startId);

            var currentId = -1;
            while (_iterations<IterationLimit && openSet.HasNext())
            {
                currentId = openSet[openSet.Pop()].Content;
                var currentNode = NodeGraph[currentId];
                
                //不使用early quit，因为会使用非最优解
                //early quit
                // if (currentId == goalId)
                // {
                //     break;
                // }
                    
                var neighboursId = new NativeList<int>(4, Allocator.Temp);
                currentNode.GetNeighbours(ref NodeGraph, ref neighboursId);

                for (var i = 0; i < neighboursId.Length; i++)
                {
                    var neighbourId = neighboursId[i];
                    var neighbourNode = NodeGraph[neighbourId];
                    //if reward == -infinity means obstacle, skip
                    if (float.IsNegativeInfinity(neighbourNode.GetReward(ref NodeGraph))) continue;

                    var newRewardSum =
                        rewardSum[currentId] + neighbourNode.GetReward(ref NodeGraph);

                    var neighbourExecutor = neighbourNode.AgentExecutorEntity;
                    var neighbourExecutorMoveSpeed = FindAgentSpeed(neighbourExecutor);
                    var neighbourTimes = CalcNeighbourTimeSum(ref TimeResult, currentId, neighbourId,
                        neighbourNode, neighbourExecutorMoveSpeed, Allocator.Temp);
                    var newLongestTime = GetLongestTime(ref neighbourTimes);

                    //如果记录已存在，新的时间更长则skip，相等则考虑reward更小skip
                    if (rewardSum.ContainsKey(neighbourId))
                    {
                        var times = TimeResult.GetValuesForKey(neighbourId);
                        var oldLongestTime = 0f;
                        foreach (var time in times)
                        {
                            if (time.TotalTime > oldLongestTime) oldLongestTime = time.TotalTime;
                        }

                        if (newLongestTime > oldLongestTime) continue;

                        var fewerReward = newRewardSum <= rewardSum[neighbourId];
                        if (Math.Abs(newLongestTime - oldLongestTime) < 0.1f && fewerReward)
                            continue;
                    }

                    //新记录更好，覆盖旧记录
                    SaveTimeSum(ref TimeResult, neighbourId, ref neighbourTimes);
                    var priority = newLongestTime -
                                   (newRewardSum + neighbourNode.Heuristic(ref NodeGraph));
                    openSet.Push(new MinHeapNode<int>(neighbourId, priority));
                    cameFrom[neighbourId] = currentId;
                    rewardSum[neighbourId] = newRewardSum;

                    neighbourTimes.Dispose();
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

        private void InitTimeSum(ref NativeMultiHashMap<int, NodeTime> timeSum, int startId)
        {
            for (var i = 0; i < AgentEntities.Length; i++)
            {
                timeSum.Add(startId, new NodeTime
                {
                    AgentEntity = AgentEntities[i],
                    EndPosition = AgentStartPositions[i],
                    TotalTime = AgentStartTime[i]
                });
            }
        }

        private NativeList<NodeTime> CalcNeighbourTimeSum(ref NativeMultiHashMap<int, NodeTime> timeSum,
            int currentNodeId, int neighbourNodeId, Node neighbourNode, float neighbourExecutorMoveSpeed, Allocator allocator)
        {
            var nodeTimes = new NativeList<NodeTime>(10, allocator);
            //遍历currentNode的各agent的Time信息
            var found = timeSum.TryGetFirstValue(currentNodeId, out var currentAgentTime, out var it);
            while (found)
            {
                //对于neighbour的执行者，进行时间计算后，再拷贝给neighbour
                //其他agent的时间信息则直接拷贝给neighbour
                if (currentAgentTime.AgentEntity.Equals(neighbourNode.AgentExecutorEntity))
                {
                    var distance = math.distance(currentAgentTime.EndPosition,
                        neighbourNode.NavigatingSubjectPosition);
                    var timeNavigate = distance / neighbourExecutorMoveSpeed;
                    var newTime = new NodeTime
                    {
                        AgentEntity = neighbourNode.AgentExecutorEntity,
                        EndPosition = neighbourNode.NavigatingSubjectPosition,
                        TotalTime = currentAgentTime.TotalTime + timeNavigate + neighbourNode.ExecuteTime,
                    };
                    nodeTimes.Add(newTime);
                }
                else
                {
                    nodeTimes.Add(currentAgentTime);
                }
                found = timeSum.TryGetNextValue(out currentAgentTime, ref it);
            }

            return nodeTimes;
        }

        private void SaveTimeSum(ref NativeMultiHashMap<int, NodeTime> timeSum, int neighbourId,
            ref NativeList<NodeTime> newTimes)
        {
            if (timeSum.ContainsKey(neighbourId))
            {
                timeSum.Remove(neighbourId);
            }

            for (var i = 0; i < newTimes.Length; i++)
            {
                timeSum.Add(neighbourId, newTimes[i]);
            }
        }

        /// <summary>
        /// 获取某一个Node中所有Agent的Time里最长的一个
        /// </summary>
        /// <param name="nodeTimes"></param>
        /// <returns></returns>
        private float GetLongestTime(ref NativeList<NodeTime> nodeTimes)
        {
            var longest = 0f;
            for (var i = 0; i < nodeTimes.Length; i++)
            {
                if (nodeTimes[i].TotalTime > longest)
                {
                    longest = nodeTimes[i].TotalTime;
                }
            }

            return longest;
        }

        private float FindAgentSpeed(Entity agentEntity)
        {
            for (var i = 0; i < AgentEntities.Length; i++)
            {
                if (!AgentEntities[i].Equals(agentEntity)) continue;
                return AgentMoveSpeeds[i].value;
            }

            return 0;
        }
    }
}