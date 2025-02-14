using Newtonsoft.Json;
using UnityEngine;

namespace GameFramework
{
    public class BuildConfig
    {
        public const string ConfigFileAssetPath = "Assets/Resources/BuildConfig.json";

        public static BuildConfigData data;

        /// <summary>
        /// 将日志输出到文件的开关，默认为false
        /// true(不再调用UntiyEnging.Log，直接将日志写到文件中)
        /// false(调用UnityEngine.Log)，
        /// </summary>
        public static bool LogToFileEnable { get; private set; }

        /// <summary>
        /// LogViewer开关:日志显示插件开关
        /// </summary>
        public static bool LogViewerEnable { get; private set; }

        /// <summary>
        /// GMUI开关:开启生成GMUI,关闭不生成GMUI
        /// </summary>
        public static bool GMUIEnable { get; private set; }

        /// <summary>
        /// Debug登录开关:开启可以登录任意玩家账号
        /// </summary>
        public static bool DebugLoginEnable { get; private set; }

        /// <summary>
        /// 切换服务器开关:开启可以登录任意玩家账号
        /// </summary>
        public static bool SwitchServerEnable { get; private set; }

        /// <summary>
        /// 内部开发包
        /// </summary>
        public static bool IsDevelop => data.packageType == PackageType.Develop;

        /// <summary>
        /// 内部测试包
        /// </summary>
        public static bool IsInternal => data.packageType == PackageType.Internal;

        /// <summary>
        /// 正式发布包
        /// </summary>
        public static bool IsPublish => data.packageType == PackageType.Publish;

        /// <summary>
        /// 外部体验包
        /// </summary>
        public static bool IsExternal => data.packageType == PackageType.External;

        /// <summary>
        /// 除了正式包，都算测试包
        /// </summary>
        public static bool IsTest => !IsPublish;

        static BuildConfig()
        {
            Initialize();
        }

        public static void Initialize()
        {
            TextAsset asset = Resources.Load<TextAsset>(System.IO.Path.GetFileNameWithoutExtension(ConfigFileAssetPath));
            data = JsonConvert.DeserializeObject<BuildConfigData>(asset.text);
            Debug.Log($"读取本地出包配置：{asset.text}");

            if (data.packageType == PackageType.Publish)
            {
                LogToFileEnable = true;
                LogViewerEnable = false;
                GMUIEnable = false;
                DebugLoginEnable = false;
                SwitchServerEnable = false;
            }
            else if (data.packageType == PackageType.Develop)
            {
                LogToFileEnable = false;
                LogViewerEnable = true;
                GMUIEnable = true;
                DebugLoginEnable = true;
                SwitchServerEnable = true;
            }
            else if (data.packageType == PackageType.Internal)
            {
                LogToFileEnable = false;
                LogViewerEnable = true;
                GMUIEnable = true;
                DebugLoginEnable = true;
                SwitchServerEnable = false;
            }
            else if (data.packageType == PackageType.External)
            {
                LogToFileEnable = false;
                LogViewerEnable = true;
                GMUIEnable = false;
                DebugLoginEnable = true;
                SwitchServerEnable = false;
            }
        }
    }
}