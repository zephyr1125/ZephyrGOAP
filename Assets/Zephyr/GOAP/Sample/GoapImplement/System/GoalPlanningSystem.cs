using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Action;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System
{
    public class GoalPlanningSystem : GoalPlanningSystemBase
    {
        protected override JobHandle ScheduleAllActionExpand(JobHandle handle, StackData stackData,
            NativeArray<ValueTuple<Entity, Node>> nodeAgentPairs, 
            NativeArray<int> existedNodesHash,
            NativeList<ValueTuple<int, State>> requires, NativeList<ValueTuple<int, State>> deltas,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter, NativeList<(int, int)>.ParallelWriter nodeToParentsWriter,
            NativeHashMap<int, State>.ParallelWriter statesWriter, NativeList<(int, int)>.ParallelWriter preconditionHashesWriter,
            NativeList<(int, int)>.ParallelWriter effectHashesWriter, NativeList<(int, int)>.ParallelWriter requireHashesWriter,
            NativeList<(int, int)>.ParallelWriter deltaHashesWriter, NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration)
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