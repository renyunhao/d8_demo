namespace GameFramework
{
    /// <summary>
    /// 包的类型
    /// </summary>
    public enum PackageType
    {
        /// <summary>
        /// 无效值
        /// </summary>
        None = 0,
        /// <summary> 内部 开发包</summary>
        Develop = 1,
        /// <summary> 内部 测试包</summary>
        Internal = 2,
        /// <summary> 正式 发布包</summary>
        Publish = 3,
        /// <summary> 外部 体验包</summary>
        External = 4,
    }
}