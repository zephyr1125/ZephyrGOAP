using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Debugger
{
    /// <summary>
    /// 考虑到多种debugger的可能性而使用接口
    /// 例如编辑器可视化debugger，运行时debugger以及单元测试中便于检测数据的debugger
    /// </summary>
    public interface IGoapDebugger
    {
        void StartLog(EntityManager entityManager, Entity agent);
        
        void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager);

        void SetPathResult(ref NativeList<Node> pathResult);

        void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager);

        void LogDone();

        void Log(string log);
    }
}