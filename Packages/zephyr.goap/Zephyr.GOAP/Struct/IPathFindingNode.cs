using Unity.Collections;

namespace Zephyr.GOAP.Struct
{
    public interface IPathFindingNode
    {
        float GetReward([ReadOnly]ref NodeGraph nodeGraph);
        
        float Heuristic([ReadOnly]ref NodeGraph nodeGraph);
        
        NativeList<int> GetNeighbours([ReadOnly]ref NodeGraph nodeGraph, Allocator allocator);
    }
}