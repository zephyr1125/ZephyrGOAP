using DOTS.Struct;
using Unity.Collections;

namespace DOTS.Debugger
{
    /// <summary>
    /// 考虑到多种debugger的可能性而使用接口
    /// 例如编辑器可视化debugger，运行时debugger以及单元测试中便于检测数据的debugger
    /// </summary>
    public interface IGoapDebugger
    {
        void SetNodeGraph(ref NodeGraph nodeGraph);

        void SetPathResult(ref NativeList<Node> pathResult);

        void Log(string log);
    }
}