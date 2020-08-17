using NUnit.Framework;
using Unity.Entities;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Tests
{
    public class TestBase
    {
        protected static World World;
        protected EntityManager EntityManager;
        protected EntityManager.EntityManagerDebug ManagerDebug;
    
        [SetUp]
        public virtual void SetUp()
        {
            World = new World("Test World");

            EntityManager = World.EntityManager;
            ManagerDebug = new EntityManager.EntityManagerDebug(EntityManager);

            var baseStateHelper = World.GetOrCreateSystem<BaseStatesHelper>();
            baseStateHelper.Update();
            baseStateHelper._removeECBufferSystem.Update();
            EntityManager.CompleteAllJobs();
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (EntityManager != null)
            {
                World.Dispose();
                EntityManager = null;
            }
        }
    }
}