namespace GameFramework.Module.TriggerSystem
{
    /// <summary>
    /// 被触发接口，用于触发器触发
    /// </summary>
    public interface IBeTriggered
    {
        /// <summary>
        /// 触发方法
        /// </summary>
        void Trigger();
    }
}