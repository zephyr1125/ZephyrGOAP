using Unity.Collections;
using Unity.Entities;
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
        
        void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager);

        void SetPathResult(EntityManager entityManager,
            ref NativeArray<Entity> pathEntities, ref NativeList<Node> pathResult);

        void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager);

        void SetNodeAgentInfos(EntityManager entityManager,
            ref NativeMultiHashMap<int, NodeAgentInfo> nodeAgentInfos);

        void SetNodeTotalTimes(ref NativeHashMap<int, float> nodeTimes);

        void SetSpecifiedPreconditions(EntityManager entityManager,
            ref NativeList<int> pathNodeSpecifiedPreconditionIndices,
            ref NativeList<State> pathNodeSpecifiedPreconditions);

        void LogDone();

        void Log(string log);
    }
}