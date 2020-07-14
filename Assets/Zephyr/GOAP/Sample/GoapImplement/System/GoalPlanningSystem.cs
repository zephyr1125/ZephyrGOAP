using Unity.Collections;
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
            ref NativeList<Node> unexpandedNodes, ref NativeArray<int> existedNodesHash, ref NativeList<(int, State)> nodeStates,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter, NativeList<(int, int)>.ParallelWriter nodeToParentsWriter, NativeList<(int, State)>.ParallelWriter nodeStatesWriter,
            NativeHashMap<int, State>.ParallelWriter statesWriter,
            NativeList<(int, int)>.ParallelWriter preconditionHashesWriter,
            NativeList<(int, int)>.ParallelWriter effectHashesWriter,
            ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration)
        {
            handle = ScheduleActionExpand<DropItemAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, statesWriter, preconditionHashesWriter, effectHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<PickItemAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, statesWriter, preconditionHashesWriter, effectHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<EatAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash, ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, statesWriter, preconditionHashesWriter, effectHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<CookAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, statesWriter, preconditionHashesWriter, effectHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<WanderAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, statesWriter, preconditionHashesWriter, effectHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<CollectAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, statesWriter, preconditionHashesWriter, effectHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<PickRawAction>(handle,ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, statesWriter, preconditionHashesWriter, effectHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            handle = ScheduleActionExpand<DropRawAction>(handle, ref stackData,
                ref unexpandedNodes, ref existedNodesHash,  ref nodeStates, nodesWriter,
                nodeToParentsWriter, nodeStatesWriter, statesWriter, preconditionHashesWriter, effectHashesWriter,
                ref newlyCreatedNodesWriter, iteration);
            
            return handle;
        }
    }
}