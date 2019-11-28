using Unity.Collections;

namespace DOTS.Struct
{
    public interface IPathFindingNode
    {
        int GetCost([ReadOnly]ref NodeGraph nodeGraph);
        
        float Heuristic([ReadOnly]ref NodeGraph nodeGraph);
        
        void GetNeighbours([ReadOnly]ref NodeGraph nodeGraph, ref NativeList<int> neighboursId);
    }
}