using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
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

        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, Entity> NodeNavigateSubjects;

        [NativeDisableParallelForRestriction]
        public NativeList<int> SpecifiedPreconditionIndices;

        [NativeDisableParallelForRestriction]
        public NativeList<State> SpecifiedPreconditions;
            
        public void Execute()
        {
            _iterations = 0;
            _pathNodeCount = 0;

            var graphSize = NodeGraph.Length();
                
            //Generate Working Containers
            var openSet = new ZephyrNativeMinHeap<int>(Allocator.Temp);
            var cameFrom = new NativeHashMap<int, int>(graphSize, Allocator.Temp);
            var rewardSum = new NativeHashMap<int, float>(graphSize, Allocator.Temp);

            // Path finding
            var startId = StartNodeId;
            var goalId = GoalNodeId;

            openSet.Add(new MinHashNode<int>(startId, 0));
            
            rewardSum[startId] = 0;
            InitNodeAgentInfos(ref NodeAgentInfos, startId);
            InitNodeTotalTimes(ref NodeTotalTimes, startId);

            var currentHash = -1;
            while (_iterations<IterationLimit && openSet.HasNext())
            {
                currentHash = openSet.PopMin().Content;
                var currentNode = NodeGraph[currentHash];
                var currentTotalTime = NodeTotalTimes[currentHash];
                
                //不使用early quit，因为会使用非最优解
                //early quit
                // if (currentId == goalId)
                // {
                //     break;
                // }
                    
                var neighboursHash = currentNode.GetNeighbours(ref NodeGraph, Allocator.Temp);

                for (var i = 0; i < neighboursHash.Length; i++)
                {
                    var neighbourHash = neighboursHash[i];
                    var neighbourNode = NodeGraph[neighbourHash];
                    //if reward == -infinity means obstacle, skip
                    if (float.IsNegativeInfinity(neighbourNode.GetReward(ref NodeGraph))) continue;

                    var newRewardSum =
                        rewardSum[currentHash] + neighbourNode.GetReward(ref NodeGraph);

                    var neighbourExecutor = neighbourNode.AgentExecutorEntity;
                    var neighbourExecutorMoveSpeed = FindAgentSpeed(neighbourExecutor);

                    //如果neighbour的precondition是泛指，而且其导航依赖precondition的话，
                    //需要寻找路径上已有的state做替代，这样才能计算导航耗时
                    //顺便保存依赖关系
                    var tempPreconditionIndices = new NativeList<int>(Allocator.Temp);
                    var tempPreconditions = new NativeList<State>(Allocator.Temp);
                    var allSpecificPreconditionsFound = GetAllSpecificPreconditions(neighbourNode,
                        currentHash, cameFrom, ref tempPreconditionIndices, ref tempPreconditions);
                    if (!allSpecificPreconditionsFound)
                    {
                        tempPreconditionIndices.Dispose();
                        tempPreconditions.Dispose();
                        continue;
                    }
                    
                    var neighbourNavigatingPosition = NeighbourNavigatingPosition(neighbourNode,
                        ref tempPreconditionIndices, ref tempPreconditions,
                        out var isNeedNavigate, out var neighbourNavigateSubject);
                    
                    var neighbourAgentsInfo = UpdateNeighbourAgentsInfo(
                        ref NodeAgentInfos, currentHash, currentTotalTime, neighbourNode,
                        neighbourNavigatingPosition, isNeedNavigate,
                        neighbourExecutorMoveSpeed, Allocator.Temp, out var newTotalTime);

                    //如果记录已存在，新的时间更长则skip，相等则考虑reward更小skip
                    if (rewardSum.ContainsKey(neighbourHash))
                    {
                        var oldTotalTime = NodeTotalTimes[neighbourHash];
                        if (newTotalTime > oldTotalTime)
                        {
                            Clear(neighbourAgentsInfo, tempPreconditionIndices, tempPreconditions);
                            continue;
                        }

                        var fewerReward = newRewardSum <= rewardSum[neighbourHash];
                        if (Math.Abs(newTotalTime - oldTotalTime) < 0.1f && fewerReward)
                        {
                            Clear(neighbourAgentsInfo, tempPreconditionIndices, tempPreconditions);
                            continue;
                        }
                    }

                    //新记录更好，覆盖旧记录
                    SaveNeighbourAgentsInfo(ref NodeAgentInfos, neighbourHash, ref neighbourAgentsInfo);
                    //覆盖总时长
                    NodeTotalTimes[neighbourHash] = newTotalTime;
                    //覆盖精确preconditions
                    ReplaceSpecifiedPreconditions(neighbourHash, ref tempPreconditionIndices,
                        ref tempPreconditions);
                    
                    var priority = newTotalTime -
                                   (newRewardSum + neighbourNode.Heuristic(ref NodeGraph));
                    openSet.Add(new MinHashNode<int>(neighbourHash, priority));
                    cameFrom[neighbourHash] = currentHash;
                    rewardSum[neighbourHash] = newRewardSum;
                    
                    NodeNavigateSubjects[neighbourHash] = neighbourNavigateSubject;

                    Clear(neighbourAgentsInfo, tempPreconditionIndices, tempPreconditions);
                }

                _iterations++;
                neighboursHash.Dispose();
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
            if (!openSet.HasNext() && currentHash != goalId)
            {
                success = false;
                log = new NativeString64("Out of openset");
            }
            if (_iterations >= IterationLimit && currentHash != goalId)
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

            void Clear(NativeList<NodeAgentInfo> neighbourAgentsInfo, NativeList<int> tempPreconditionIndices,
                NativeList<State> tempPreconditions)
            {
                neighbourAgentsInfo.Dispose();
                tempPreconditionIndices.Dispose();
                tempPreconditions.Dispose();
            }
        }
        
        private bool GetAllSpecificPreconditions(Node neighbourNode, int currentHash,
            NativeHashMap<int, int> cameFrom,
            ref NativeList<int> tempSpecifiedPreconditionIndices,
            ref NativeList<State> tempSpecifiedPreconditions)
        {
            var allSpecificFound = true;
            var preconditions =
                NodeGraph.GetNodePreconditions(neighbourNode, Allocator.Temp);
            for(var preconditionId = 0; preconditionId < preconditions.Length(); preconditionId++)
            {
                var precondition = preconditions[preconditionId];
                tempSpecifiedPreconditionIndices.Add(neighbourNode.HashCode);
                
                if (!precondition.IsScopeState())
                {
                    tempSpecifiedPreconditions.Add(precondition);
                }
                else
                {
                    //往子追溯寻找对应的具体effect以替代宽泛precondition
                    //构建子节点列表
                    var childrenHash = new NativeList<int>(Allocator.Temp);
                    var nodeHash = currentHash;
                    childrenHash.Add(nodeHash);
                    while (cameFrom.ContainsKey(nodeHash))
                    {
                        nodeHash = cameFrom[nodeHash];
                        childrenHash.Add(nodeHash);
                    }

                    var childSpecificEffect = default(State);
                    for (var childId = 0; childId < childrenHash.Length; childId++)
                    {
                        var childHash = childrenHash[childId];
                        var childEffects = NodeGraph.GetNodeEffects(NodeGraph[childHash],
                            Allocator.Temp);
                        foreach (var childEffect in childEffects)
                        {
                            if (!childEffect.BelongTo(precondition)) continue;
                            childSpecificEffect = childEffect;
                            break;
                        }

                        childEffects.Dispose();
                        if (!childSpecificEffect.Equals(default(State))) break;
                    }

                    //如果没有找到明确的state，这是一条死路
                    var notFound = childSpecificEffect.Equals(default);
                    if (notFound) allSpecificFound = false;
                    
                    tempSpecifiedPreconditions.Add(childSpecificEffect);
                    childrenHash.Dispose();
                }
            }
            preconditions.Dispose();
            return allSpecificFound;
        }

        private void ReplaceSpecifiedPreconditions(int nodeHash,
            ref NativeList<int> newPreconditionIndices, ref NativeList<State> newPreconditions)
        {
            for (var i = 0; i < SpecifiedPreconditionIndices.Length; i++)
            {
                if (!SpecifiedPreconditionIndices[i].Equals(nodeHash)) continue;
                SpecifiedPreconditionIndices.RemoveAtSwapBack(i);
                SpecifiedPreconditions.RemoveAtSwapBack(i);
            }
            SpecifiedPreconditionIndices.AddRange(newPreconditionIndices);
            SpecifiedPreconditions.AddRange(newPreconditions);
        }

        private float3 NeighbourNavigatingPosition(Node neighbourNode,
            ref NativeList<int> preconditionIndices, ref NativeList<State> preconditions,
            out bool isNeedNavigate, out Entity navigateSubject)
        {
            float3 neighbourNavigatingPosition;
            switch (neighbourNode.NavigatingSubjectType)
            {
                case NodeNavigatingSubjectType.PreconditionTarget:
                {
                    //找到关键precondition
                    var navigateSubjectId = neighbourNode.NavigatingSubjectId;
                    var precondition = State.Null;
                    //subjectId标识的是第几个precondition的target为导航目标，因此需要计数直到找到
                    var preconditionCount = -1;
                    for (var i = 0; i < preconditionIndices.Length; i++)
                    {
                        if (!preconditionIndices[i].Equals(neighbourNode.HashCode)) continue;
                        preconditionCount++;
                        if (preconditionCount != navigateSubjectId) continue;
                        precondition = preconditions[i];
                    }
                    Assert.AreNotEqual(State.Null, precondition);
                
                    neighbourNavigatingPosition = precondition.Position;
                    navigateSubject = precondition.Target;
                    isNeedNavigate = true;
                    break;
                }
                case NodeNavigatingSubjectType.EffectTarget:
                {
                    var neighbourEffects =
                        NodeGraph.GetNodeEffects(neighbourNode, Allocator.Temp);
                    var effect = neighbourEffects[neighbourNode.NavigatingSubjectId];
                    neighbourNavigatingPosition = effect.Position;
                    navigateSubject = effect.Target;
                    isNeedNavigate = true;
                    neighbourEffects.Dispose();
                    break;
                }
                default:
                    neighbourNavigatingPosition = float3.zero;
                    navigateSubject = Entity.Null;
                    isNeedNavigate = false;
                    break;
            }

            return neighbourNavigatingPosition;
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
        /// <param name="neighbourNavigatingPosition"></param>
        /// <param name="isNeedNavigate"></param>
        /// <param name="neighbourExecutorMoveSpeed"></param>
        /// <param name="allocator"></param>
        /// <param name="newTotalTime"></param>
        /// <returns>新的agents信息</returns>
        private NativeList<NodeAgentInfo> UpdateNeighbourAgentsInfo(ref NativeMultiHashMap<int, NodeAgentInfo> agentsInfoOnNode,
            int currentNodeId, float currentTotalTime, Node neighbourNode, float3 neighbourNavigatingPosition, bool isNeedNavigate,
            float neighbourExecutorMoveSpeed, Allocator allocator, out float newTotalTime)
        {
            var agentsInfo = new NativeList<NodeAgentInfo>(allocator);
            var estimateTotalTime = 0f;

            //遍历currentNode的各agent的信息
            var found = agentsInfoOnNode.TryGetFirstValue(currentNodeId, out var currentNodeAgentInfo, out var it);
            while (found)
            {
                //对于neighbour的执行者，进行时间计算后，再拷贝给neighbour
                //其他agent的时间信息则直接拷贝给neighbour
                if (currentNodeAgentInfo.AgentEntity.Equals(neighbourNode.AgentExecutorEntity))
                {
                    var distance = 0f;
                    if(isNeedNavigate){
                        distance = math.distance(currentNodeAgentInfo.EndPosition,
                        neighbourNavigatingPosition);
                    }
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
                        EndPosition = neighbourNavigatingPosition,
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