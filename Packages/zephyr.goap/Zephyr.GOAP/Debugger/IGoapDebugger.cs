using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Logger;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Debugger
{
    /// <summary>
    /// 考虑到多种debugger的可能性而使用接口
    /// 例如编辑器可视化debugger，运行时debugger以及单元测试中便于检测数据的debugger
    /// </summary>
    public interface IGoapDebugger
    {
        void StartLog(EntityManager entityManager);
        
        void SetNodeGraph(NodeGraph nodeGraph, EntityManager entityManager);

        void SetPathResult(EntityManager entityManager,
            NativeArray<Entity> pathEntities, NativeList<Node> pathResult);

        void SetBaseStates(StateGroup baseStates, EntityManager entityManager);

        void SetNodeAgentInfos(EntityManager entityManager,
            NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos);

        void SetNodeTotalTimes(NativeHashMap<int, float> nodeTimes);

        void SetSpecifiedPreconditions(EntityManager entityManager,
            NativeList<int> pathNodeSpecifiedPreconditionIndices,
            NativeList<State> pathNodeSpecifiedPreconditions);

        void SetRewardSum(NativeHashMap<int, float> rewardSum);

        void LogDone();

        void Log(string log);

        void LogWarning(string log);

        void LogPerformance(string log);

        GoapLog GetLog();

        void SetWriteFile(bool isWriteFile);

        void SetPlanSuccess(bool isSuccess);

        bool IsPlanSuccess();
    }
}