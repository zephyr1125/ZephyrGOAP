using System;
using Unity.Entities;

namespace DOTS.Logger
{
    public class EntityView
    {
        public int Index;
        public int Version;
        public string Name;

        public EntityView(EntityManager entityManager, Entity entity)
        {
            Index = entity.Index;
            Version = entity.Version;
            Name = entityManager.GetName(entity);
        }
    }
}