using System;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class NodeDependencyLog
    {
        public int baseNodeHash, dependencyNodeHash;

        public NodeDependencyLog(int baseNodeHash, int dependencyNodeHash)
        {
            this.baseNodeHash = baseNodeHash;
            this.dependencyNodeHash = dependencyNodeHash;
        }
    }
}