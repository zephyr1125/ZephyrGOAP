using Unity.Entities;

namespace DOTS.Component.Actions
{
    public struct Action : IBufferElementData
    {
        public NativeString64 ActionJobName;
    }
}