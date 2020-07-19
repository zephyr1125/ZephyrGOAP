using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Sample.GoapImplement.System.SensorSystem
{
    /// <summary>
    /// 检测所有Recipe并存入BaseState
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class RecipeSensorSystem : ComponentSystem
    {
        
        protected override void OnUpdate()
        {
            if (!EntityManager.Exists(BaseStatesHelper.BaseStatesEntity)) return;
            
            //范例只有一个recipe，直接手写，实际需要从define里批量写入
            var buffer = EntityManager.GetBuffer<State>(BaseStatesHelper.BaseStatesEntity);
            //存储recipe这样复杂state的折中方案：
            //每个recipe以1个output+2个input的方式保存，占用连续的3个state
            //对于不需要第二个原料的recipe,其第二个input为空state
            AddRecipeState(buffer, StringTable.Instance().RoastPeachName, 1,
                StringTable.Instance().RawPeachName, 1);
            AddRecipeState(buffer, StringTable.Instance().RoastAppleName, 1,
                StringTable.Instance().RawAppleName, 1);
            AddRecipeState(buffer, StringTable.Instance().FeastName, 1,
                StringTable.Instance().RawAppleName, 1,
                StringTable.Instance().RawPeachName, 1);
        }

        /// <summary>
        /// 存储recipe这样复杂state的折中方案
        /// 每个recipe以1个output+2个input的方式保存，占用连续的3个state
        /// 对于不需要第二个原料的recipe,其第二个input为空state
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="outputName"></param>
        /// <param name="outputAmount"></param>
        /// <param name="input1Name"></param>
        /// <param name="input1Amount"></param>
        /// <param name="input2Name"></param>
        /// <param name="input2Amount"></param>
        private void AddRecipeState(DynamicBuffer<State> buffer, NativeString32 outputName, byte outputAmount,
            NativeString32 input1Name, byte input1Amount, NativeString32 input2Name = default, byte input2Amount = 0)
        {
            buffer.Add(new State
            {
                Trait = ComponentType.ReadOnly<RecipeOutputTrait>(),
                ValueTrait = ComponentType.ReadOnly<CookerTrait>(),    //以ValueTrait保存此recipe适用的生产设施
                ValueString = outputName,
                Amount = outputAmount
            });
            buffer.Add(new State
            {
                Trait = ComponentType.ReadOnly<RecipeInputTrait>(),
                ValueTrait = ComponentType.ReadOnly<CookerTrait>(),
                ValueString = input1Name,
                Amount = input1Amount
            });
            if(input2Name.Equals(default))
            {
                buffer.Add(default);
            }else
            {
                buffer.Add(new State{
                    Trait = ComponentType.ReadOnly<RecipeInputTrait>(),
                    ValueTrait = ComponentType.ReadOnly<CookerTrait>(),
                    ValueString = input2Name,
                    Amount = input2Amount
                });
            }
        }
    }
}