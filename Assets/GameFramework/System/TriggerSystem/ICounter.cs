namespace GameFramework.Module.TriggerSystem
{
    /// <summary>
    /// 计数器
    /// </summary>
    public interface ICounter
    {
        /// <summary>
        /// 绑定计数器
        /// 在方法实现中恰当的时机调用被计数对象的计数修改或设置方法，实现计数器更新。
        /// </summary>
        /// <param name="beCounted">被计数对象</param>
        void BindCounter(IBeCounted beCounted);
        /// <summary>
        /// 设置计数器参数
        /// </summary>
        /// <param name="param">参数</param>
        void SetParams(params object[] param);
    }
}