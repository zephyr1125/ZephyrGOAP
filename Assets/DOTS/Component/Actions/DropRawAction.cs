using DOTS.Component.Trait;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Component.Actions
{
    public struct DropRawAction : IComponentData, IAction
    {
        /// <summary>
        /// 条件：自体要有对应物品
        /// </summary>
        /// <param name="goalState"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        public void GetPreconditions([ReadOnly]State goalState,
            [ReadOnly]ref StackData stackData, ref StateGroup preconditions)
        {
            //只针对物品请求的goal state
            if (goalState.Trait != typeof(Inventory)) return;

            var agent = stackData.AgentEntity;
            
            preconditions.Add(new State
            {
                Target = agent,
                Trait = typeof(Inventory),
                StringValue = goalState.StringValue
            });
        }

        /// <summary>
        /// 效果：目标获得对应物品
        /// </summary>
        /// <param name="goalState"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        public void GetEffects([ReadOnly]State goalState,
            [ReadOnly]ref StackData stackData, ref StateGroup effects)
        {
            //只针对物品请求的goal state
            if (goalState.Trait != typeof(Inventory)) return;
            
            effects.Add(new State
            {
                Target = goalState.Target,
                Trait = typeof(Inventory),
                StringValue = goalState.StringValue
            });
        }

        public void CreateNodes([ReadOnly] ref StateGroup goalStates,
            [ReadOnly] ref StackData stackData, int jobIndex,
            EntityCommandBuffer.Concurrent ecBuffer)
        {
            //对于drop raw而言
            //对于每种agent拥有并且goal需要的raw，产生一个Node
            foreach (var goalState in goalStates)
            {
                //只针对物品请求的goal state
                if (goalState.Trait != typeof(Inventory)) continue;

                var rawRequiredName = goalState.StringValue;
                
                var preconditions = new StateGroup(1, Allocator.Temp);
                var effects = new StateGroup(1, Allocator.Temp);
                
                GetPreconditions(goalState, ref stackData,
                    ref preconditions);
                GetEffects(goalState, ref stackData, ref effects);
                
                //预先应有一个SelfInventorySensor把自己背包信息装入settings
                foreach (var setting in stackData.Settings)
                {
                    if(setting.Target != stackData.AgentEntity) continue;
                    if (setting.Trait != typeof(Inventory)) continue;
                    if (!rawRequiredName.Equals(setting.StringValue)) continue;

                    var nodeEntity = ecBuffer.CreateEntity(jobIndex);
                    ecBuffer.AddComponent<Node>(jobIndex, nodeEntity);
                    //将变更后的states存入新node
                    var newStates = new StateGroup(goalStates, Allocator.Temp);
                    newStates.Merge(preconditions);
                    newStates.Sub(effects);
                    var buffer = ecBuffer.AddBuffer<State>(jobIndex, nodeEntity);
                    foreach (var newState in newStates)
                    {
                        buffer.Add(newState);
                    }
                    newStates.Dispose();
                }
                
                preconditions.Dispose();
                effects.Dispose();
            }
        }
    }
}