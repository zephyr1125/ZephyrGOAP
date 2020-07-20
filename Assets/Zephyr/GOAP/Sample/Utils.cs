using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Sample
{
    public static class Utils
    {
        /// <summary>
        /// 根据传入的配方输出筛选，传出其对应的输入State组
        /// 能根据输出的数量要求给出成倍计算后的输入数量，如果出现配方产量超过需求，就产生富余
        /// </summary>
        /// <param name="stateGroup"></param>
        /// <param name="recipeOutFilter"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static StateGroup GetRecipeInputInStateGroup(StateGroup stateGroup, State recipeOutFilter, Allocator allocator)
        {
            
            var result = new StateGroup(2, allocator);
            for (var i = 0; i < stateGroup.Length(); i++)
            {
                if (stateGroup[i].BelongTo(recipeOutFilter))
                {
                    
                    var multiply = math.ceil((float)recipeOutFilter.Amount / stateGroup[i].Amount);
                    var input1 = stateGroup[i + 1];
                    var input2 = stateGroup[i + 2];
                    input1.Amount *= (byte) multiply;
                    input2.Amount *= (byte) multiply;
                    result.Add(input1);
                    result.Add(input2);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 示例用的方法，根据trait获取所有对应物品名，实际应从define获取
        /// </summary>
        /// <param name="trait"></param>
        /// <param name="itemNames"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static NativeList<NativeString32> GetItemNamesOfSpecificTrait(int trait,
            NativeHashMap<int, NativeString32> itemNames, Allocator allocator)
        {
            var result = new NativeList<NativeString32>(allocator);
            
            if (trait.Equals(TypeManager.GetTypeIndex<FoodTrait>()))
            {
                result.Add(itemNames[(int)ItemName.RawPeach]);
                result.Add(itemNames[(int)ItemName.RoastPeach]);
                result.Add(itemNames[(int)ItemName.RawApple]);
                result.Add(itemNames[(int)ItemName.RoastApple]);
                result.Add(itemNames[(int)ItemName.Feast]);
            }

            return result;
        }

        public const float RawPeachStamina = 0.2f;
        public const float RawAppleStamina = 0.3f;
        public const float RoastPeachStamina = 0.4f;
        public const float RoastAppleStamina = 0.5f;
        public const float FeastStamina = 2;

        /// <summary>
        /// 示例用的方法，获取不同食物的食用reward
        /// </summary>
        /// <param name="foodName"></param>
        /// <param name="itemNames"></param>
        /// <returns></returns>
        public static float GetFoodReward(NativeString32 foodName,
            NativeHashMap<int, NativeString32> itemNames)
        {
            var plus = 10;
            if (foodName.Equals(itemNames[(int)ItemName.RawPeach]))
            {
                return RawPeachStamina*plus;
            }
            if (foodName.Equals(itemNames[(int)ItemName.RoastPeach]))
            {
                return RoastPeachStamina*plus;
            }
            if (foodName.Equals(itemNames[(int)ItemName.RawApple]))
            {
                return RawAppleStamina*plus;
            }
            if (foodName.Equals(itemNames[(int)ItemName.RoastApple]))
            {
                return RoastAppleStamina*plus;
            }
            if (foodName.Equals(itemNames[(int)ItemName.Feast]))
            {
                return FeastStamina*plus;
            }
            return 0;
        }
        
        public static float GetFoodStamina(NativeString32 foodName)
        {
            if (foodName.Equals(ItemNames.Instance().RawPeachName))
            {
                return RawPeachStamina;
            }
            if (foodName.Equals(ItemNames.Instance().RoastPeachName))
            {
                return RoastPeachStamina;
            }
            if (foodName.Equals(ItemNames.Instance().RawAppleName))
            {
                return RawAppleStamina;
            }
            if (foodName.Equals(ItemNames.Instance().RoastAppleName))
            {
                return RoastAppleStamina;
            }
            if (foodName.Equals(ItemNames.Instance().FeastName))
            {
                return FeastStamina;
            }

            return 0;
        }
    }
}