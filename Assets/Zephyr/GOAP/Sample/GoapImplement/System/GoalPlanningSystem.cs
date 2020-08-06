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
        private NativeHashMap<int, FixedString32> _itemNames;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            _itemNames = new NativeHashMap<int, FixedString32>(5, Allocator.Persistent);
            _itemNames.Add((int)ItemName.RawPeach, ItemNames.Instance().RawPeachName);
            _itemNames.Add((int)ItemName.RoastPeach, ItemNames.Instance().RoastPeachName);
            _itemNames.Add((int)ItemName.RawApple, ItemNames.Instance().RawAppleName);
            _itemNames.Add((int)ItemName.RoastApple, ItemNames.Instance().RoastAppleName);
            _itemNames.Add((int)ItemName.Feast, ItemNames.Instance().FeastName);
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
            stackData.ItemNames = _itemNames;
            
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

        protected override void OnDestroy()
        {
            _itemNames.Dispose();
        }
    }
}