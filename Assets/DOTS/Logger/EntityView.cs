using System;
using Unity.Entities;

namespace DOTS.Logger
{
    [Serializable]
    public class EntityView
    {
        public int Index;
        public int Version;
        public string Name;

        public EntityView(EntityManager entityManager, Entity entity)
        {
            Index = entity.Index;
            Version = entity.Version;
#if UNITY_EDITOR
            Name = entityManager.GetName(entity);
#endif
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name)) return Name;
            return $"{Index},{Version}";
        }

        public bool Equals(Entity entity)
        {
            return Index == entity.Index && Version == entity.Version;
        }
    }
}