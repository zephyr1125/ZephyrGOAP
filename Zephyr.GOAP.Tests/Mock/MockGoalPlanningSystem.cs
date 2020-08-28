using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Tests.Mock
{
    [DisableAutoCreation]
    public class MockGoalPlanningSystem : GoalPlanningSystemBase
    {
        private NativeHashMap<int, FixedString32> _itemNames;

        protected override void OnCreate()
        {
            base.OnCreate();
            _itemNames = new NativeHashMap<int, FixedString32>(5, Allocator.Persistent);
            _itemNames.Add(0, "mock_item");
        }

        protected override JobHandle ScheduleAllActionExpand(JobHandle handle, StackData stackData, NativeArray<ZephyrValueTuple<Entity, Node>> nodeAgentPairs,
            NativeArray<int> existedNodesHash, NativeList<ZephyrValueTuple<int, State>> requires, NativeList<ZephyrValueTuple<int, State>> deltas,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter, NativeList<ZephyrValueTuple<int, int>>.ParallelWriter nodeToParentsWriter, NativeHashMap<int, State>.ParallelWriter statesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter preconditionHashesWriter, NativeList<ZephyrValueTuple<int, int>>.ParallelWriter effectHashesWriter,
            NativeList<ZephyrValueTuple<int, int>>.ParallelWriter requireHashesWriter, NativeList<ZephyrValueTuple<int, int>>.ParallelWriter deltaHashesWriter,
            NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration)
        {
            stackData.ItemNames = _itemNames;
            
            handle = ScheduleActionExpand<MockProduceAction>(handle, stackData,
                nodeAgentPairs, existedNodesHash, requires, deltas, nodesWriter, nodeToParentsWriter, 
                statesWriter, preconditionHashesWriter, effectHashesWriter, requireHashesWriter, deltaHashesWriter,
                newlyCreatedNodesWriter, iteration);
            return handle;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _itemNames.Dispose();
        }
    }
}