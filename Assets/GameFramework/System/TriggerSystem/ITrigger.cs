namespace GameFramework.Module.TriggerSystem
{
    /// <summary>
    /// 触发器
    /// </summary>
    public interface ITrigger
    {
        /// <summary>
        /// 绑定触发器
        /// 在方法实现中设置恰当的时机调用被触发对象的触发方法，实现特定事件触发。
        /// </summary>
        /// <param name="beTriggered">被触发对象</param>
        void BindTrigger(IBeTriggered beTriggered);

        /// <summary>
        /// 设置触发器参数
        /// </summary>
        /// <param name="param">参数</param>
        void SetParams(params object[] param);
    }
}