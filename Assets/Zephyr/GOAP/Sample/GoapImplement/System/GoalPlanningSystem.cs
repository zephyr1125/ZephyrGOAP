using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System
{
    public class GoalPlanningSystem : GoalPlanningSystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            StringTable.Instance().Init();
            Debug.Log(StringTable.Instance().FeastName);
        }

        protected override JobHandle ScheduleAllActionExpand(JobHandle handle, StackData stackData,
            NativeArray<ZephyrValueTuple<Entity, Node>> nodeAgentPairs, 
            NativeArray<int> existedNodesHash,
            NativeList<ZephyrValueTuple<int, State>> requires, NativeList<ZephyrValueTuple<int, State>> deltas,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter, NativeList<ZephyrValueTuple<int, int>>.ParallelWriter nodeToParentsWriter,
            NativeHashMap<int, State>.ParallelWriter statesWriter, NativeList<ZephyrValueTuple<int, int>>.ParallelWriter preconditionHashesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter effectHashesWriter, NativeList<ZephyrValueTuple<int, int>>.ParallelWriter requireHashesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter deltaHashesWriter, NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration)
        {
             handle = ScheduleActionExpand<DropItemAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<PickItemAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<EatAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<CookAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<WanderAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<CollectAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<PickRawAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<DropRawAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            
            return handle;
        }
    }
}