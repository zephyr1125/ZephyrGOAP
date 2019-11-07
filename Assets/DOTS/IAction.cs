using Unity.Collections;
using Unity.Entities;

namespace DOTS
{
    public interface IAction
    {
        void GetPreconditions(
            [ReadOnly]State goalState,
            [ReadOnly]ref StackData stackData,
            ref StateGroup preconditions);

        void GetEffects(
            [ReadOnly]State goalState,
            [ReadOnly]ref StackData stackData,
            ref StateGroup effects);
        
        /// <summary>
        /// 基于当前外界数据，产生多个对应node
        /// </summary>
        void CreateNodes([ReadOnly]ref StateGroup goalStates,
            [ReadOnly]ref StackData stackData, int jobIndex,
            EntityCommandBuffer.Concurrent ECBuffer);
    }
}