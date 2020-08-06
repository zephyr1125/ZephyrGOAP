using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Tests.Mock;
using Assert = Unity.Assertions.Assert;

namespace Zephyr.GOAP.Tests
{
    public class TestState
    {
        private State _aState, _bState;

        [SetUp]
        public void SetUp()
        {
            _aState = new State {Target = new Entity{Index = 1, Version = 0}, Trait = TypeManager.GetTypeIndex<MockTraitA>()};
        }

        [Test]
        public void IsCountable_Amount()
        {
            Assert.IsFalse(_aState.IsCountable());                              

            _aState.Amount = 3;
            Assert.IsTrue(_aState.IsCountable());
        }

        /// <summary>
        /// 两者都可数，数量不同，则Same
        /// </summary>
        [Test]
        public void SameTo_BothCountable_DifferentAmount_True()
        {
            _aState.Amount = 1;
            _bState = new State {Target = new Entity{Index = 1, Version = 0}, Trait = TypeManager.GetTypeIndex<MockTraitA>(), Amount = 2};

            Assert.IsTrue(_aState.SameTo(_bState));
        }

        /// <summary>
        /// 如果两者的Amount分属可数与不可数，那么不能Same
        /// </summary>
        [Test]
        public void SameTo_CountableDifferent_False()
        {
            _bState = new State {Target = new Entity{Index = 1, Version = 0}, Trait = TypeManager.GetTypeIndex<MockTraitA>(), Amount = 3};
            Assert.IsFalse(_aState.SameTo(_bState));
        }

        [Test]
        public void Equal_AllEqual_True()
        {
            _bState = new State {Target = new Entity{Index = 1, Version = 0}, Trait = TypeManager.GetTypeIndex<MockTraitA>()};
            Assert.IsTrue(_aState.Equals(_bState));
        }
    }
}