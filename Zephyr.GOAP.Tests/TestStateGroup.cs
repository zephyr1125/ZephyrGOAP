using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;
using Zephyr.GOAP.Tests.Mock;
using Assert = Unity.Assertions.Assert;

namespace Zephyr.GOAP.Tests
{
    public class TestStateGroup
    {
        private StateGroup _aStates, _bStates;

        [SetUp]
        public void SetUp()
        {
            _aStates = new StateGroup(2, Allocator.Temp)
            {
                new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitA)},
                new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 3}
            };

            _bStates = new StateGroup(1, Allocator.Temp);
        }

        [TearDown]
        public void TearDown()
        {
            _aStates.Dispose();
            _bStates.Dispose();
        }
        
        #region OR

        //对于不可数state,仅比对是否存在,已存在的不处理
        [Test]
        public void OR_NonCountable_Exist_Nothing()
        {
            _bStates.Add(new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitA)});
            _aStates.OR(_bStates);
            
            Assert.AreEqual(2, _aStates.Length());
            Assert.AreEqual(new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitA)},
                _aStates[0]);
            Assert.AreEqual(new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 3},
                _aStates[1]);
        }

        //对于不可数state,仅比对是否存在,不存在的则追加
         [Test]
         public void OR_NonCountable_NotExist_Append()
         {
             var state = new State
                 {Target = new Entity {Index = 2, Version = 0}, Trait = typeof(MockTraitA)};
             _bStates.Add(state);
             _aStates.OR(_bStates);
             
             Assert.AreEqual(3, _aStates.Length());
             Assert.AreEqual(state, _aStates[2]);
         }
        
        //对于可数的state,如果Same，累加amount
        [Test]
        public void OR_Countable_Same_AddAmount()
        {
            _bStates.Add(new State{Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 2});
            
            _aStates.OR(_bStates);
            Assert.AreEqual(2, _aStates.Length());
            Assert.AreEqual(5, _aStates[1].Amount);
        }
        
        
        //仅仅belongTo不能累加数量，只能追加
        [Test]
        public void OR_BelongTo_Append()
        {
            _bStates.Add(new State{Trait = typeof(MockTraitB), Amount = 2});
            
            _aStates.OR(_bStates);
            Assert.AreEqual(3, _aStates.Length());
            Assert.AreEqual(3, _aStates[1].Amount);
            Assert.AreEqual(2, _aStates[2].Amount);
        }

        [Test]
        public void OR_MultiStates()
        {
            var equalState = new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitA)};
            var newState = new State {Target = new Entity {Index = 2, Version = 0}, Trait = typeof(MockTraitA)};
            var sameState = new State{Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 2};
            _bStates.Add(equalState);
            _bStates.Add(newState);
            _bStates.Add(sameState);
            
            _aStates.OR(_bStates);
            Assert.AreEqual(3, _aStates.Length());
            Assert.AreEqual(newState, _aStates[2]);
            Assert.AreEqual(5, _aStates[1].Amount);
        }

        #endregion

        #region AND

        //不可数左右Equal的，移除左侧
        [Test]
        public void AND_NonCountable_Equal_RemoveLeft()
        {
            _bStates.Add(new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitA)});
            
            _aStates.AND(_bStates);
            Assert.AreEqual(1, _aStates.Length());
            Assert.AreEqual(typeof(MockTraitB), _aStates[0].Trait);
        }

        //右 belong to 左，移除左侧
        [Test]
        public void AND_NonCountable_BelongTo_RemoveLeft()
        {
            _bStates.Add(new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitA), ValueString = "a"});
            
            _aStates.AND(_bStates);
            Assert.AreEqual(1, _aStates.Length());
            Assert.AreEqual(typeof(MockTraitB), _aStates[0].Trait);
        }
        
        //可数左右Same的，减少左侧数量
        [Test]
        public void AND_Countable_Same_ReduceLeft() {
            _bStates.Add(new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 1});
            
            _aStates.AND(_bStates);
            Assert.AreEqual(2, _aStates.Length());
            Assert.AreEqual(2, _aStates[1].Amount);
        }

        //可数右 belong to 左，减少左侧数量
        [Test]
        public void AND_Countable_BelongTo_ReduceLeft()
        {
            _bStates.Add(new State {Target = new Entity{Index = 1, Version = 0},
                Trait = typeof(MockTraitB), ValueString = "b", Amount = 1});
            
            _aStates.AND(_bStates);
            Assert.AreEqual(2, _aStates.Length());
            Assert.AreEqual(2, _aStates[1].Amount);
        }
        
        //可数右侧有多个符合的，要多次减少数量
        [Test]
        public void AND_Countable_MultiFit_ReduceLeft()
        {
            _bStates.Add(new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 1});
            _bStates.Add(new State {Target = new Entity{Index = 1, Version = 0},
                Trait = typeof(MockTraitB), ValueString = "b", Amount = 1});
            
            _aStates.AND(_bStates);
            Assert.AreEqual(2, _aStates.Length());
            Assert.AreEqual(1, _aStates[1].Amount);
        }

        //对于在And中被完全满足的可数State，要直接移除，以避免变成Amount=0与不可数State混淆
        [Test]
        public void AND_Zero_Countable_RemoveState()
        {
            _bStates.Add(new State {Target = new Entity{Index = 1, Version = 0},
                Trait = typeof(MockTraitB), ValueString = "b", Amount = 3});
            
            _aStates.AND(_bStates);
            Assert.AreEqual(1, _aStates.Length());
            Assert.AreEqual(typeof(MockTraitA), _aStates[0].Trait);
        }

        //多个state的整体测试
        [Test]
        public void AND_MultiState()
        {
            var belongState = new State {Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitA), ValueString = "b"};
            var sameState = new State{Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 2};
            _bStates.Add(belongState);
            _bStates.Add(sameState);
            
            _aStates.AND(_bStates);
            Assert.AreEqual(1, _aStates.Length());
            Assert.AreEqual(1, _aStates[0].Amount);
            Assert.AreEqual(2, _bStates.Length());
            Assert.AreEqual(belongState, _bStates[0]);
        }

        //要输出被移除了的右侧state
        [Test]
        public void AND_OutputOtherRemovedStates()
        {
            var sameState = new State{Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 2};
            _bStates.Add(sameState);

            var removedOther = _aStates.AND(_bStates, true);
            Assert.AreEqual(1, removedOther.Length());
            Assert.AreEqual(sameState, removedOther[0]);
            removedOther.Dispose();
        }
        
        //要输出被减少的右侧state的正确数量
        [Test]
        public void AND_OutputOtherReducedAmount()
        {
            var sameState = new State{Target = new Entity{Index = 1, Version = 0}, Trait = typeof(MockTraitB), Amount = 5};
            _bStates.Add(sameState);

            var removedOther = _aStates.AND(_bStates, true);
            Assert.AreEqual(1, removedOther.Length());
            Assert.IsTrue(removedOther[0].SameTo(sameState));
            Assert.AreEqual(2, removedOther[0].Amount);
            removedOther.Dispose();
        }
        
        #endregion
        
    }
}