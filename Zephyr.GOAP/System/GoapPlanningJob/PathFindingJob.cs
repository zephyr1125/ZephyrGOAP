using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoapPlanningJob
{
    [BurstCompile]
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
        public NativeArray<AgentMoveSpeed> AgentMoveSpeeds;

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
        
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> RewardSum;
            
        public void Execute()
        {
            _iterations = 0;
            _pathNodeCount = 0;

            var graphSize = NodeGraph.Length();
                
            //Generate Working Containers
            var openSet = new ZephyrNativeMinHeap<int>(Allocator.Temp);
            var cameFrom = new NativeHashMap<int, int>(graphSize, Allocator.Temp);

            // Path finding
            var startId = StartNodeId;
            var goalId = GoalNodeId;

            openSet.Add(new MinHashNode<int>(startId, 0));
            
            RewardSum[startId] = 0;
            InitNodeAgentInfos(NodeAgentInfos, startId);
            InitNodeTotalTimes(NodeTotalTimes, startId);

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
                    
                var neighboursHash = currentNode.GetNeighbours(NodeGraph, Allocator.Temp);

                for (var i = 0; i < neighboursHash.Length; i++)
                {
                    var neighbourHash = neighboursHash[i];
                    var neighbourNode = NodeGraph[neighbourHash];
                    //if reward == -infinity means obstacle, skip
                    if (float.IsNegativeInfinity(neighbourNode.GetReward(NodeGraph))) continue;

                    var newRewardSum =
                        RewardSum[currentHash] + neighbourNode.GetReward(NodeGraph);

                    var neighbourExecutor = neighbourNode.AgentExecutorEntity;
                    var neighbourExecutorMoveSpeed = FindAgentSpeed(neighbourExecutor);

                    //如果neighbour的precondition是泛指，而且其导航依赖precondition的话，
                    //需要寻找路径上已有的state做替代，这样才能计算导航耗时
                    //顺便保存依赖关系
                    var tempPreconditionIndices = new NativeList<int>(Allocator.Temp);
                    var tempPreconditions = new NativeList<State>(Allocator.Temp);
                    var dependentTime = GetAllSpecificPreconditions(neighbourNode,
                        currentHash, cameFrom, tempPreconditionIndices, tempPreconditions);

                    var neighbourNavigatingPosition = NeighbourNavigatingPosition(neighbourNode,
                        tempPreconditionIndices, tempPreconditions,
                        out var isNeedNavigate, out var neighbourNavigateSubject);
                    
                    var neighbourAgentsInfo = UpdateNeighbourAgentsInfo(
                        NodeAgentInfos, currentHash, currentTotalTime, neighbourNode,
                        neighbourNavigatingPosition, isNeedNavigate, dependentTime,
                        neighbourExecutorMoveSpeed, Allocator.Temp, out var newTotalTime);

                    //如果记录已存在，新的时间更长则skip，相等则考虑reward更小skip
                    if (RewardSum.ContainsKey(neighbourHash))
                    {
                        var oldTotalTime = NodeTotalTimes[neighbourHash];
                        if (newTotalTime > oldTotalTime)
                        {
                            Clear(neighbourAgentsInfo, tempPreconditionIndices, tempPreconditions);
                            continue;
                        }

                        var fewerReward = newRewardSum <= RewardSum[neighbourHash];
                        if (Math.Abs(newTotalTime - oldTotalTime) < 0.1f && fewerReward)
                        {
                            Clear(neighbourAgentsInfo, tempPreconditionIndices, tempPreconditions);
                            continue;
                        }
                    }

                    //新记录更好，覆盖旧记录
                    SaveNeighbourAgentsInfo(NodeAgentInfos, neighbourHash, neighbourAgentsInfo);
                    //覆盖总时长
                    NodeTotalTimes[neighbourHash] = newTotalTime;
                    //覆盖精确preconditions
                    ReplaceSpecifiedPreconditions(neighbourHash, tempPreconditionIndices,
                        tempPreconditions);
                    
                    var priority = newTotalTime -
                                   (newRewardSum + neighbourNode.Heuristic(NodeGraph));
                    openSet.Add(new MinHashNode<int>(neighbourHash, priority));
                    cameFrom[neighbourHash] = currentHash;
                    RewardSum[neighbourHash] = newRewardSum;
                    
                    NodeNavigateSubjects[neighbourHash] = neighbourNavigateSubject;

                    Clear(neighbourAgentsInfo, tempPreconditionIndices, tempPreconditions);
                }

                _iterations++;
                neighboursHash.Dispose();
            }
                
            //Construct path
            var nodeId = cameFrom[goalId];    //goalId不记录
            while (_pathNodeCount < PathNodeLimit && !nodeId.Equals(startId))
            {
                Result.Add(NodeGraph[nodeId]);
                nodeId = cameFrom[nodeId];
                _pathNodeCount++;
            }
                
            //Log Result
            // var success = true;
            // var log = new NativeString32("Path finding success");
            // if (!openSet.HasNext() && currentHash != goalId)
            // {
            //     success = false;
            //     log = "Out of openset";
            // }
            // if (_iterations >= IterationLimit && currentHash != goalId)
            // {
            //     success = false;
            //     log = "Iteration limit reached";
            // }else if (_pathNodeCount >= PathNodeLimit && !nodeId.Equals(startId))
            // {
            //     success = false;
            //     log = "Step limit reached";
            // }
                
            //Clear
            openSet.Dispose();
            cameFrom.Dispose();

            void Clear(NativeList<NodeAgentInfo> neighbourAgentsInfo, NativeList<int> tempPreconditionIndices,
                NativeList<State> tempPreconditions)
            {
                neighbourAgentsInfo.Dispose();
                tempPreconditionIndices.Dispose();
                tempPreconditions.Dispose();
            }
        }
        
        private float GetAllSpecificPreconditions(Node neighbourNode, int currentHash,
            NativeHashMap<int, int> cameFrom,
            NativeList<int> tempSpecifiedPreconditionIndices,
            NativeList<State> tempSpecifiedPreconditions)
        {
            var dependentTime = 0f;
            var preconditions =
                NodeGraph.GetPreconditions(neighbourNode, Allocator.Temp);
            for(var preconditionId = 0; preconditionId < preconditions.Length(); preconditionId++)
            {
                var precondition = preconditions[preconditionId];
                tempSpecifiedPreconditionIndices.Add(neighbourNode.HashCode);

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
                    var childEffects = NodeGraph.GetEffects(NodeGraph[childHash],
                        Allocator.Temp);
                    for (var effectId = 0; effectId < childEffects.Length(); effectId++)
                    {
                        var childEffect = childEffects[effectId];
                        if (!childEffect.BelongTo(precondition)) continue;
                        childSpecificEffect = childEffect;
                        //数量需要以precondition为准，因为start的effect里的数量是全部现场数量
                        childSpecificEffect.Amount = precondition.Amount;
                        break;
                    }

                    childEffects.Dispose();
                    if (!childSpecificEffect.Equals(default))
                    {
                        var time = NodeTotalTimes[childHash];
                        if(dependentTime<time)dependentTime = time;
                        break;
                    }
                }
                childrenHash.Dispose();
                
                if (!precondition.IsScopeState())
                {
                    tempSpecifiedPreconditions.Add(precondition);
                }
                else
                {
                    //不可能找不到明确的state，否则是不会从start一路连上来的
                    Assert.IsFalse(childSpecificEffect.Equals(default));
                    tempSpecifiedPreconditions.Add(childSpecificEffect);
                }
            }
            preconditions.Dispose();
            return dependentTime;
        }

        private void ReplaceSpecifiedPreconditions(int nodeHash,
            NativeList<int> newPreconditionIndices, NativeList<State> newPreconditions)
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
            NativeList<int> preconditionIndices, NativeList<State> preconditions,
            out bool isNeedNavigate, out Entity navigateSubject)
        {
            float3 neighbourNavigatingPosition;
            switch (neighbourNode.NavigatingSubjectType)
            {
                case NodeNavigatingSubjectType.PreconditionTarget:
                {
                    //找到关键precondition
                    var navigateSubjectId = neighbourNode.NavigatingSubjectId;
                    var precondition = default(State);
                    //subjectId标识的是第几个precondition的target为导航目标，因此需要计数直到找到
                    var preconditionCount = -1;
                    for (var i = 0; i < preconditionIndices.Length; i++)
                    {
                        if (!preconditionIndices[i].Equals(neighbourNode.HashCode)) continue;
                        preconditionCount++;
                        if (preconditionCount != navigateSubjectId) continue;
                        precondition = preconditions[i];
                    }
                    //todo
                    Assert.AreNotEqual(default, precondition);
                
                    neighbourNavigatingPosition = precondition.Position;
                    navigateSubject = precondition.Target;
                    isNeedNavigate = true;
                    break;
                }
                case NodeNavigatingSubjectType.EffectTarget:
                {
                    var neighbourEffects =
                        NodeGraph.GetEffects(neighbourNode, Allocator.Temp);
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

        private void InitNodeAgentInfos(NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos, int startId)
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
        
        private void InitNodeTotalTimes(NativeHashMap<int, float> nodeTotalTimes, int startId)
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
        /// <param name="dependentTime"></param>
        /// <param name="neighbourExecutorMoveSpeed"></param>
        /// <param name="allocator"></param>
        /// <param name="newTotalTime"></param>
        /// <returns>新的agents信息</returns>
        private NativeList<NodeAgentInfo> UpdateNeighbourAgentsInfo(NativeMultiHashMap<int, NodeAgentInfo> agentsInfoOnNode,
            int currentNodeId, float currentTotalTime, Node neighbourNode, float3 neighbourNavigatingPosition, bool isNeedNavigate,
            float dependentTime, float neighbourExecutorMoveSpeed, Allocator allocator, out float newTotalTime)
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

                    //如果我之前的availableTime累加上我的timeNavigate还小于dependentTime的话
                    //说明我已经闲置蛮久了，直接以dependentTime为开始execute的时间
                    //也就是说有足够的时间提前navigate
                    var estimateExecuteStartTime =
                        currentNodeAgentInfo.AvailableTime + timeNavigate;
                    if (estimateExecuteStartTime < dependentTime)
                        estimateExecuteStartTime = dependentTime;
                    
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

        private void SaveNeighbourAgentsInfo(NativeMultiHashMap<int, NodeAgentInfo> agentsInfo, int neighbourId,
            NativeList<NodeAgentInfo> newInfos)
        {
            Assert.IsTrue(newInfos.Length > 0);
            
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