namespace ReGoap.Core
{
    public interface IReGoapMemory<T,W>
    {
        /// <summary>
        /// 是否是指最终goal?
        /// </summary>
        /// <returns></returns>
        ReGoapState<T, W> GetWorldState();
    }
}