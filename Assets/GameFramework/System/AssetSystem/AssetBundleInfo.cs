namespace GameFramework
{
    public class AssetBundleInfo
    {
        /// <summary>
        /// 资源完整的名称 包含后缀名
        /// </summary>
        public string assetType;

        /// <summary>
        /// 资源包的名称
        /// </summary>
        public string assetBundleName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="assetType">资源类型</param>
        /// <param name="assetBundleName">资源包的名称</param>
        public AssetBundleInfo(string assetType, string assetBundleName)
        {
            this.assetType = assetType;
            this.assetBundleName = assetBundleName;
        }
    }
}