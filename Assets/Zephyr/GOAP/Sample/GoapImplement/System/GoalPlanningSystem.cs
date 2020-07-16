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
        protected override JobHandle ScheduleAllActionExpand(JobHandle handle, ref StackData stackData,
            NativeArray<ValueTuple<Entity, Node>> nodeAgentPairs, 
            ref NativeArray<int> existedNodesHash,
            NativeList<ValueTuple<int, State>> requires, NativeList<ValueTuple<int, State>> deltas,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter, NativeList<(int, int)>.ParallelWriter nodeToParentsWriter,
            NativeHashMap<int, State>.ParallelWriter statesWriter, NativeList<(int, int)>.ParallelWriter preconditionHashesWriter,
            NativeList<(int, int)>.ParallelWriter effectHashesWriter, NativeList<(int, int)>.ParallelWriter requireHashesWriter,
            NativeList<(int, int)>.ParallelWriter deltaHashesWriter, ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration)
        {
             handle = ScheduleActionExpand<DropItemAction>(handle, ref stackData,
                nodeAgentPairs, ref existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<PickItemAction>(handle, ref stackData,
                nodeAgentPairs, ref existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<EatAction>(handle, ref stackData,
                nodeAgentPairs, ref existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<CookAction>(handle, ref stackData,
                nodeAgentPairs, ref existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<WanderAction>(handle, ref stackData,
                nodeAgentPairs, ref existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<CollectAction>(handle, ref stackData,
                nodeAgentPairs, ref existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<PickRawAction>(handle, ref stackData,
                nodeAgentPairs, ref existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<DropRawAction>(handle, ref stackData,
                nodeAgentPairs, ref existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            
            return handle;
        }
    }
}