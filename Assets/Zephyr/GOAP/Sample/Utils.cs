using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Sample
{
    public static class Utils
    {
        public static readonly NativeString32 RawPeachName = "raw_peach";
        public static readonly NativeString32 RoastPeachName = "roast_peach";
        public static readonly NativeString32 RawAppleName = "raw_apple";
        public static readonly NativeString32 RoastAppleName = "roast_apple";
        public static readonly NativeString32 FeastName = "feast";
        
        /// <summary>
        /// 根据传入的配方输出筛选，传出其对应的输入State组
        /// </summary>
        /// <param name="currentStates"></param>
        /// <param name="recipeOutFilter"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static StateGroup GetRecipeInputInCurrentStates(ref StateGroup currentStates, State recipeOutFilter, Allocator allocator)
        {
            var result = new StateGroup(2, allocator);
            for (var i = 0; i < currentStates.Length(); i++)
            {
                if (currentStates[i].BelongTo(recipeOutFilter))
                {
                    result.Add(currentStates[i+1]);
                    result.Add(currentStates[i+2]);
                    break;
                }
            }

            return result;
        }
        
        /// <summary>
        /// 示例用的方法，根据trait获取所有对应物品名，实际应从define获取
        /// </summary>
        /// <param name="trait"></param>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public static NativeList<NativeString32> GetItemNamesOfSpecificTrait(ComponentType trait,
            Allocator allocator)
        {
            var result = new NativeList<NativeString32>(allocator);
            
            if (trait.Equals(typeof(FoodTrait)))
            {
                result.Add(RawPeachName);
                result.Add(RoastPeachName);
                result.Add(RawAppleName);
                result.Add(RoastAppleName);
                result.Add(FeastName);
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
        /// <returns></returns>
        public static float GetFoodReward(NativeString32 foodName)
        {
            var plus = 10;
            if (foodName.Equals(RawPeachName))
            {
                return RawPeachStamina*plus;
            }
            if (foodName.Equals(RoastPeachName))
            {
                return RoastPeachStamina*plus;
            }
            if (foodName.Equals(RawAppleName))
            {
                return RawAppleStamina*plus;
            }
            if (foodName.Equals(RoastAppleName))
            {
                return RoastAppleStamina*plus;
            }
            if (foodName.Equals(FeastName))
            {
                return FeastStamina*plus;
            }
            return 0;
        }
        
        public static float GetFoodStamina(NativeString32 foodName)
        {
            if (foodName.Equals(RawPeachName))
            {
                return RawPeachStamina;
            }
            if (foodName.Equals(RoastPeachName))
            {
                return RoastPeachStamina;
            }
            if (foodName.Equals(RawAppleName))
            {
                return RawAppleStamina;
            }
            if (foodName.Equals(RoastAppleName))
            {
                return RoastAppleStamina;
            }
            if (foodName.Equals(FeastName))
            {
                return FeastStamina;
            }

            return 0;
        }
    }
}