using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Sample.GoapImplement.Component.Trait;
using Zephyr.GOAP.Struct;
using Assert = Unity.Assertions.Assert;

namespace Zephyr.GOAP.Sample.Tests
{
    public class TestUtils
    {
        private StateGroup _baseStates;

        [SetUp]
        public void SetUp()
        {
            _baseStates = new StateGroup(3, Allocator.Temp);
            _baseStates.Add(new State
            {
                Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),    //以ValueTrait保存此recipe适用的生产设施
                ValueString = "output",
                Amount = 2
            });
            _baseStates.Add(new State
            {
                Trait = TypeManager.GetTypeIndex<RecipeInputTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                ValueString = "input1",
                Amount = 1
            });
            _baseStates.Add(new State
            {
                Trait = TypeManager.GetTypeIndex<RecipeInputTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                ValueString = "input2",
                Amount = 3
            });
        }

        [TearDown]
        public void TearDown()
        {
            _baseStates.Dispose();
        }
        
        //在获取配方时要根据输入的成品数量自动乘到输出的材料数量上
        [Test]
        public void GetRecipeInputInBaseStates_MultiplyInputAmount()
        {
            var output = new State
            {
                Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                ValueString = "output",
                Amount = 6
            };

            var inputs =
                Utils.GetRecipeInputInStateGroup(_baseStates, output, Allocator.Temp, out var outputAmount);

            Assert.AreEqual(3, inputs[0].Amount);
            Assert.AreEqual(9, inputs[1].Amount);
            
            inputs.Dispose();
        }
        
        /// <summary>
        /// 如果需求不足一次，也生产一次
        /// </summary>
        [Test]
        public void GetRecipeInputInBaseStates_AtLeastOnce()
        {
            var output = new State
            {
                Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                ValueString = "output",
                Amount = 1
            };

            var inputs =
                Utils.GetRecipeInputInStateGroup(_baseStates, output, Allocator.Temp, out var outputAmount);

            Assert.AreEqual(1, inputs[0].Amount);
            Assert.AreEqual(3, inputs[1].Amount);
            
            inputs.Dispose();
        }

        /// <summary>
        /// 如果出现配方产量超过需求，就产生富余
        /// </summary>
        [Test]
        public void GetRecipeInputInBaseStates_AdditionAmount()
        {
            var output = new State
            {
                Trait = TypeManager.GetTypeIndex<RecipeOutputTrait>(),
                ValueTrait = TypeManager.GetTypeIndex<CookerTrait>(),
                ValueString = "output",
                Amount = 3
            };

            var inputs =
                Utils.GetRecipeInputInStateGroup(_baseStates, output, Allocator.Temp, out var outputAmount);

            Assert.AreEqual(2, inputs[0].Amount);
            Assert.AreEqual(6, inputs[1].Amount);
            
            inputs.Dispose();
        }
    }
}