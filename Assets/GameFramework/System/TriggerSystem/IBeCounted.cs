namespace GameFramework.Module.TriggerSystem
{
    /// <summary>
    /// 被计数对象，主要用于实现计数器时的接口参数。
    /// </summary>
    public interface IBeCounted
    {
        /// <summary>
        /// 修改计数值
        /// </summary>
        /// <param name="offset">偏移量</param>
        void ModifyCount(int offset);
        /// <summary>
        /// 设置计数值
        /// </summary>
        /// <param name="value">计数值</param>
        void SetCount(int value);
    }
}