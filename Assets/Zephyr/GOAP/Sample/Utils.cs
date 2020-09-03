using Unity.Collections;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.Game.Component;
using Zephyr.GOAP.Sample.Game.Component.Order;
using Zephyr.GOAP.Sample.Game.Component.Order.OrderState;
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
        /// 如果输入的output数量为0，则假定为一次生产，并输出产物数量
        /// </summary>
        /// <param name="stateGroup"></param>
        /// <param name="recipeOutputFilter"></param>
        /// <param name="allocator"></param>
        /// <param name="outputAmount">输出的outputAmount，在已经提供amount时为原值，没提供的话为一次生产的值</param>
        /// <returns></returns>
        public static StateGroup GetRecipeInputInStateGroup(StateGroup stateGroup, State recipeOutputFilter,
            Allocator allocator, out int outputAmount)
        {
            outputAmount = recipeOutputFilter.Amount;
            var result = new StateGroup(2, allocator);
            for (var i = 0; i < stateGroup.Length(); i++)
            {
                if (stateGroup[i].BelongTo(recipeOutputFilter))
                {
                    //如果输入的output数量为0，则假定为一次生产，并输出产物数量
                    if (outputAmount == 0) outputAmount = stateGroup[i].Amount;
                    
                    var multiply = math.ceil((float)outputAmount / stateGroup[i].Amount);
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
        public static NativeList<FixedString32> GetItemNamesOfSpecificTrait(int trait,
            NativeHashMap<int, FixedString32> itemNames, Allocator allocator)
        {
            var result = new NativeList<FixedString32>(allocator);
            
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
        public static float GetFoodReward(FixedString32 foodName,
            NativeHashMap<int, FixedString32> itemNames)
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
        
        public static float GetFoodStamina(FixedString32 foodName)
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

        /// <summary>
        /// 变动容器内的物品数量
        /// </summary>
        /// <param name="entityInQueryIndex"></param>
        /// <param name="containerEntity"></param>
        /// <param name="ecb"></param>
        /// <param name="containedItemRefBuffer"></param>
        /// <param name="allCounts"></param>
        /// <param name="itemName"></param>
        /// <param name="amount">正为增加，负为移除</param>
        /// <returns>true:成功，增加物品总是成功的，false:失败，移除时没有对应物品或者数量不足</returns>
        public static bool ModifyItemInContainer(int entityInQueryIndex, EntityCommandBuffer.ParallelWriter ecb,
            Entity containerEntity, DynamicBuffer<ContainedItemRef> containedItemRefBuffer,
            ComponentDataFromEntity<Count> allCounts,
            FixedString32 itemName, int amount)
        {
            if (amount > 0)
            {
                return AddItemToContainer(entityInQueryIndex, ecb,
                    containerEntity, containedItemRefBuffer, allCounts, itemName, amount);
            }
            if (amount < 0)
            {
                return RemoveItemFromContainer(entityInQueryIndex, ecb, containedItemRefBuffer,
                    allCounts, itemName, amount);
            }

            return true;
        }

        private static bool AddItemToContainer(int entityInQueryIndex, EntityCommandBuffer.ParallelWriter ecb, 
            Entity containerEntity, DynamicBuffer<ContainedItemRef> containedItemRefBuffer,
            ComponentDataFromEntity<Count> allCounts, FixedString32 itemName, int amount)
        {
            var existed = false;
            for (var containedItemId = 0; containedItemId < containedItemRefBuffer.Length; containedItemId++)
            {
                var itemRef = containedItemRefBuffer[containedItemId];
                if (!itemRef.ItemName.Equals(itemName)) continue;
                var itemEntity = itemRef.ItemEntity;
                var originalAmount = allCounts[itemEntity].Value;
                ecb.SetComponent(entityInQueryIndex, itemEntity,
                    new Count {Value = (byte) (originalAmount + amount)});
                existed = true;
            }

            if (!existed)
            {
                var itemEntity = ecb.CreateEntity(entityInQueryIndex);
                ecb.AddComponent(entityInQueryIndex, itemEntity, new Item());
                ecb.AddComponent(entityInQueryIndex, itemEntity, new Name {Value = itemName});
                ecb.AddComponent(entityInQueryIndex, itemEntity, new Count {Value = (byte)amount});

                ecb.AppendToBuffer(entityInQueryIndex, containerEntity,
                    new ContainedItemRef {ItemName = itemName, ItemEntity = itemEntity});
            }

            return true;
        }

        /// <summary>
        /// 从物品容器里移除一定数量的指定物品
        /// </summary>
        /// <param name="entityInQueryIndex"></param>
        /// <param name="ecb"></param>
        /// <param name="containedItemRefBuffer"></param>
        /// <param name="allCounts"></param>
        /// <param name="itemName"></param>
        /// <param name="amount">始终为负值</param>
        /// <returns>true:成功，false:失败，没有对应物品或者数量不足</returns>
        private static bool RemoveItemFromContainer(int entityInQueryIndex,
            EntityCommandBuffer.ParallelWriter ecb,
            DynamicBuffer<ContainedItemRef> containedItemRefBuffer,
            ComponentDataFromEntity<Count> allCounts,
            FixedString32 itemName, int amount)
        {
            for (var containedItemId = 0; containedItemId < containedItemRefBuffer.Length; containedItemId++)
            {
                var itemRef = containedItemRefBuffer[containedItemId];
                if (!itemRef.ItemName.Equals(itemName)) continue;
                var itemEntity = itemRef.ItemEntity;
                var originalAmount = allCounts[itemEntity].Value;
                Assert.IsTrue(originalAmount >= -amount);
                ecb.SetComponent(entityInQueryIndex, itemEntity,
                    new Count {Value = (byte) (originalAmount + amount)});
                return true;
            }

            return false;    //没找到物品
        }
        
        public static void NextOrderState<T, TU>(Entity orderEntity, int jobIndex,
            EntityCommandBuffer.ParallelWriter eCBuffer) 
            where T : struct, IComponentData, IOrderState where TU : struct, IComponentData, IOrderState
        {
            eCBuffer.RemoveComponent<T>(jobIndex, orderEntity);
            eCBuffer.AddComponent(jobIndex, orderEntity, new TU());
        }
        
        public static void OrderExecuteStart<T>(Order order, ComponentDataFromEntity<T> actions, Entity orderEntity,
            int entityInQueryIndex, EntityCommandBuffer.ParallelWriter ecb, double time) where T:struct, IComponentData, IAction
        {
            var executorEntity = order.ExecutorEntity;
            //获取执行时间
            var setting = new State
            {
                Trait = TypeManager.GetTypeIndex<ItemSourceTrait>(),
                ValueString = order.ItemName
            };
            var actionPeriod = actions[executorEntity].GetExecuteTime(setting);

            //todo 播放动态

            NextOrderState<OrderReadyToExecute, OrderExecuting>(orderEntity,
                entityInQueryIndex, ecb);
            ecb.AddComponent(entityInQueryIndex, orderEntity,
                new OrderExecuteTime {ExecutePeriod = actionPeriod, StartTime = time});
        }
    }
}