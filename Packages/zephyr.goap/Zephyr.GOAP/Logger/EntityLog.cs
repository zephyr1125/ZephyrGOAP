using System;
using Unity.Entities;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class EntityLog : IComparable<EntityLog>
    {
        public int index;
        public int version;
        public string name;

        public EntityLog(EntityManager entityManager, Entity entity)
        {
            index = entity.Index;
            version = entity.Version;
#if UNITY_EDITOR
            name = entityManager.GetName(entity);
#endif
        }

        public int CompareTo(EntityLog other)
        {
            return index.CompareTo(other.index);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(name)) return name;
            return $"{index},{version}";
        }
        
        public bool Equals(EntityLog entityLog)
        {
            return index == entityLog.index && version == entityLog.version;
        }

        public bool Equals(Entity entity)
        {
            return index == entity.Index && version == entity.Version;
        }
    }
}