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

        /// <summary>
        /// 各Node的估算起始导航时间
        /// </summary>
        public NativeHashMap<int, float>.ParallelWriter NodesEstimateNavigateTimeWriter;

        /// <summary>
        /// 各node的total time
        /// </summary>
        public NativeHashMap<int, float> NodeTotalTimes;
        /// <summary>
        /// 各node上的各agent的信息
        /// </summary>
        public NativeMultiHashMap<int, NodeAgentInfo> NodeAgentInfos;
            
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
            InitNodeAgentInfos(ref NodeAgentInfos, startId);
            InitNodeTotalTimes(ref NodeTotalTimes, startId);

            var currentId = -1;
            while (_iterations<IterationLimit && openSet.HasNext())
            {
                currentId = openSet[openSet.Pop()].Content;
                var currentNode = NodeGraph[currentId];
                var currentTotalTime = NodeTotalTimes[currentId];
                
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
                    var neighbourAgentsInfo = UpdateNeighbourAgentsInfo(
                        ref NodeAgentInfos, currentId, currentTotalTime, neighbourNode, neighbourExecutorMoveSpeed,
                        Allocator.Temp, out var newTotalTime);
                    

                    //如果记录已存在，新的时间更长则skip，相等则考虑reward更小skip
                    if (rewardSum.ContainsKey(neighbourId))
                    {
                        var oldTotalTime = NodeTotalTimes[neighbourId];
                        if (newTotalTime > oldTotalTime) continue;

                        var fewerReward = newRewardSum <= rewardSum[neighbourId];
                        if (Math.Abs(newTotalTime - oldTotalTime) < 0.1f && fewerReward)
                            continue;
                    }

                    //新记录更好，覆盖旧记录
                    SaveNeighbourAgentsInfo(ref NodeAgentInfos, neighbourId, ref neighbourAgentsInfo);
                    //覆盖总时长
                    NodeTotalTimes[neighbourId] = newTotalTime;
                    
                    var priority = newTotalTime -
                                   (newRewardSum + neighbourNode.Heuristic(ref NodeGraph));
                    openSet.Push(new MinHeapNode<int>(neighbourId, priority));
                    cameFrom[neighbourId] = currentId;
                    rewardSum[neighbourId] = newRewardSum;

                    neighbourAgentsInfo.Dispose();
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

        private void InitNodeAgentInfos(ref NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos, int startId)
        {
            for (var i = 0; i < AgentEntities.Length; i++)
            {
                nodeAgentInfos.Add(startId, new NodeAgentInfo
                {
                    AgentEntity = AgentEntities[i],
                    EndPosition = AgentStartPositions[i],
                    NavigateTime = AgentStartTime[i]
                });
            }
        }
        
        private void InitNodeTotalTimes(ref NativeHashMap<int, float> nodeTotalTimes, int startId)
        {
            nodeTotalTimes.Add(startId, 0);
        }

        /// <summary>
        /// 更新各个agent在指定neighbour node上的信息
        /// </summary>
        /// <param name="agentsInfoOnNode"></param>
        /// <param name="currentNodeId"></param>
        /// <param name="currentTotalTime"></param>
        /// <param name="neighbourNode"></param>
        /// <param name="neighbourExecutorMoveSpeed"></param>
        /// <param name="allocator"></param>
        /// <param name="newTotalTime"></param>
        /// <returns>新的agents信息</returns>
        private NativeList<NodeAgentInfo> UpdateNeighbourAgentsInfo(ref NativeMultiHashMap<int, NodeAgentInfo> agentsInfoOnNode,
            int currentNodeId, float currentTotalTime, Node neighbourNode,
            float neighbourExecutorMoveSpeed, Allocator allocator, out float newTotalTime)
        {
            var agentsInfo = new NativeList<NodeAgentInfo>(10, allocator);
            var estimateTotalTime = 0f;

            //遍历currentNode的各agent的信息
            var found = agentsInfoOnNode.TryGetFirstValue(currentNodeId, out var currentNodeAgentInfo, out var it);
            while (found)
            {
                //对于neighbour的执行者，进行时间计算后，再拷贝给neighbour
                //其他agent的时间信息则直接拷贝给neighbour
                if (currentNodeAgentInfo.AgentEntity.Equals(neighbourNode.AgentExecutorEntity))
                {
                    var distance = math.distance(currentNodeAgentInfo.EndPosition,
                        neighbourNode.NavigatingSubjectPosition);
                    var timeNavigate = distance / neighbourExecutorMoveSpeed;

                    //如果我之前的availableTime累加上我的timeNavigate还小于当前的totalTime的话
                    //说明我已经闲置蛮久了，直接以当前totalTime为开始execute的时间
                    //也就是说有足够的时间提前navigate
                    var estimateExecuteStartTime =
                        currentNodeAgentInfo.AvailableTime + timeNavigate;
                    if (estimateExecuteStartTime < currentTotalTime)
                        estimateExecuteStartTime = currentTotalTime;
                    
                    NodesEstimateNavigateTimeWriter.TryAdd(neighbourNode.HashCode, estimateExecuteStartTime - timeNavigate);
                    
                    var newInfo = new NodeAgentInfo
                    {
                        AgentEntity = neighbourNode.AgentExecutorEntity,
                        EndPosition = neighbourNode.NavigatingSubjectPosition,
                        NavigateTime = timeNavigate,
                        ExecuteTime = neighbourNode.ExecuteTime,
                        AvailableTime = estimateExecuteStartTime + neighbourNode.ExecuteTime,
                    };
                    agentsInfo.Add(newInfo);
                    estimateTotalTime = newInfo.AvailableTime;
                }
                else
                {
                    var newInfo = currentNodeAgentInfo;
                    newInfo.ExecuteTime = 0;
                    newInfo.NavigateTime = 0;
                    agentsInfo.Add(newInfo);
                }
                found = agentsInfoOnNode.TryGetNextValue(out currentNodeAgentInfo, ref it);
            }

            newTotalTime = estimateTotalTime;
            return agentsInfo;
        }

        private void SaveNeighbourAgentsInfo(ref NativeMultiHashMap<int, NodeAgentInfo> agentsInfo, int neighbourId,
            ref NativeList<NodeAgentInfo> newInfos)
        {
            if (agentsInfo.ContainsKey(neighbourId))
            {
                agentsInfo.Remove(neighbourId);
            }

            for (var i = 0; i < newInfos.Length; i++)
            {
                agentsInfo.Add(neighbourId, newInfos[i]);
            }
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