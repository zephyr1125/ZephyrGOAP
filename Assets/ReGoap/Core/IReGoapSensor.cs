namespace ReGoap.Core
{
    public interface IReGoapSensor<T,W>
    {
        void Init(IReGoapMemory<T, W> memory);
        IReGoapMemory<T, W> GetMemory();
        void UpdateSensor();
    }
}