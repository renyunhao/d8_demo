using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameFramework
{
    public class GameBuilder
    {
        public const string PlatformAndroid = "android";
        public const string PlatformWindows = "win";
        public const string PlatformIOS = "ios";
        public const string OutputDirectory = "../../output";
        public const string PipelineDirectory = "../../pipeline";

        private static string windowsFilePath;
        private static string androidProjectDirectory;
        private static string iosProjectDirectory;
        private static PackageType cachedPackageType;

        public static void BuildWindows(PackageType type)
        {
            cachedPackageType = type;
            ExportWindowsEXE(type);
        }

        public static void BuildAPK(PackageType type)
        {
            cachedPackageType = type;
            if (ExportAndroidProject(type))
            {
                BuildAndroidProject();
            }
        }

        public static void BuildIPA(PackageType type)
        {
            cachedPackageType = type;
            if (ExportIOSProject(type))
            {
                BuildIOSProject();
            }
        }

        public static void ExportAndroidProjectByCurrentConfig()
        {
            BuildConfigData config = GetDefaultConfigData();
            ExportAndroidProject(config.packageType);
        }

        public static void ExportIOSProjectByCurrentConfig()
        {
            BuildConfigData config = GetDefaultConfigData();
            ExportIOSProject(config.packageType);
        }

        public static void MergeAndroidProject()
        {
            string commandPath = Path.Combine(Path.GetFullPath("."), PipelineDirectory);
            Process p = new Process();
            p.StartInfo.FileName = commandPath + "/gradlew";
            p.StartInfo.WorkingDirectory = commandPath;
            p.StartInfo.Arguments = "mergeAndroidProject";
            p.Start();
            p.WaitForExit();
        }

        public static void MergeIOSProject()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                string commandPath = Path.Combine(Path.GetFullPath("."), PipelineDirectory);
                var startInfo = new ProcessStartInfo
                {
                    FileName = commandPath + "/gradlew",
                    WorkingDirectory = commandPath,
                    Arguments = "mergeIOSProject",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                var p = Process.Start(startInfo);
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    string normalOutput = p.StandardOutput.ReadToEnd();
                    Debug.Log("MergeIOSProject 成功: " + normalOutput);
                }
                else
                {
                    string errorOutput = p.StandardError.ReadToEnd();
                    Debug.Log("MergeIOSProject 失败: " + errorOutput);
                }
            }
        }

        public static void BuildAndroidProject()
        {
            if (cachedPackageType == PackageType.None)
            {
                string path = Path.Combine(Path.GetFullPath("."), OutputDirectory, Path.GetFileNameWithoutExtension(BuildConfig.ConfigFileAssetPath) + ".txt");
                string[] outputBuildConfig = File.ReadAllLines(path);
                cachedPackageType = (PackageType)Enum.Parse(typeof(PackageType), outputBuildConfig[2]);
                if (cachedPackageType == PackageType.None)
                {
                    return;
                }
            }
            string typeName = cachedPackageType.ToString().ToLower();
            string channelName = "";
            string channelOverrideFilePath = Path.Combine(Application.dataPath, "ResourcesRaw/BuildChannel.txt");
            if (File.Exists(channelOverrideFilePath))
            {
                channelName = File.ReadAllText(channelOverrideFilePath).Trim();
            }
            Debug.Log($"BuildAndroidProject channel: {channelName}");

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                string commandPath = Path.Combine(Path.GetFullPath("."), PipelineDirectory);
                Process p = new Process();
                p.StartInfo.FileName = commandPath + $"/skip_unity_build_apk_{typeName}.bat";
                p.StartInfo.WorkingDirectory = commandPath;
                p.StartInfo.Arguments = channelName;
                p.Start();
            }
            else
            {
                string commandFilePath = Path.Combine(Path.GetFullPath("."), PipelineDirectory, $"skip_unity_build_apk_{typeName}.sh");
                Process.Start("/bin/bash", "-c \" chmod +x " + commandFilePath + " \"");
                var startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = "open";
                startInfo.Arguments = "-a Terminal.app " + commandFilePath;
                startInfo.WorkingDirectory = Path.GetDirectoryName(commandFilePath);
                Process.Start(startInfo);
            }
        }

        public static void BuildIOSProject()
        {
            if (cachedPackageType == PackageType.None)
            {
                string path = Path.Combine(Path.GetFullPath("."), OutputDirectory, Path.GetFileNameWithoutExtension(BuildConfig.ConfigFileAssetPath) + ".txt");
                string[] outputBuildConfig = File.ReadAllLines(path);
                cachedPackageType = (PackageType)Enum.Parse(typeof(PackageType), outputBuildConfig[2]);
                if (cachedPackageType == PackageType.None)
                {
                    return;
                }
            }
            string typeName = cachedPackageType.ToString().ToLower();
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                string commandFilePath = Path.Combine(Path.GetFullPath("."), PipelineDirectory, $"skip_unity_build_ipa_{typeName}.sh");
                Process.Start("/bin/bash", "-c \" chmod +x " + commandFilePath + " \"");
                var startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = "open";
                startInfo.Arguments = "-a Terminal.app " + commandFilePath;
                startInfo.WorkingDirectory = Path.GetDirectoryName(commandFilePath);
                Process.Start(startInfo);
            }
        }

        public static BuildConfigData GetDefaultConfigData()
        {
            string sourceFile = Path.Combine(Path.GetFullPath("."), BuildConfig.ConfigFileAssetPath);
            string content = File.ReadAllText(sourceFile);
            return JsonConvert.DeserializeObject<BuildConfigData>(content);
        }

        private static bool ExportWindowsEXE(PackageType type)
        {
            PrepareDirectory(BuildTarget.StandaloneWindows64);
            UpdateBuildConfig(type);
            AssetDatabase.Refresh();

            bool debugUIEnabled = type != PackageType.Publish;
            GameBuilderProcess.ProcessSymbols(BuildTarget.StandaloneWindows64, debugUIEnabled);
            GameBuilderProcess.ProcessStreamingAssets(BuildTarget.StandaloneWindows64);
            var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, windowsFilePath, BuildTarget.StandaloneWindows64, BuildOptions.None);
            //调试时使用下面的代码
            //var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, windowsFilePath, BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.ConnectWithProfiler);

            GameBuilderProcess.RevertSymbolsModification(BuildTarget.StandaloneWindows64);
            GameBuilderProcess.RevertStreamingAssetsModification();
            RestoreBuildConfig();
            AssetDatabase.Refresh();
            if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ExportAndroidProject(PackageType type)
        {
            PrepareDirectory(BuildTarget.Android);
            UpdateBuildConfig(type);
            AssetDatabase.Refresh();

            bool debugUIEnabled = type != PackageType.Publish;
            GameBuilderProcess.ProcessSymbols(BuildTarget.Android, debugUIEnabled);
            GameBuilderProcess.ProcessStreamingAssets(BuildTarget.Android);

            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Debugging;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, androidProjectDirectory, BuildTarget.Android, BuildOptions.AcceptExternalModificationsToPlayer | BuildOptions.CompressWithLz4HC);
            //调试时使用下面的代码
            //var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, androidProjectDirectory, BuildTarget.Android, BuildOptions.AcceptExternalModificationsToPlayer | BuildOptions.Development | BuildOptions.ConnectWithProfiler);

            GameBuilderProcess.RevertSymbolsModification(BuildTarget.Android);
            GameBuilderProcess.RevertStreamingAssetsModification();
            RestoreBuildConfig();

            AssetDatabase.Refresh();
            if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                MergeAndroidProject();
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ExportIOSProject(PackageType type)
        {
            PrepareDirectory(BuildTarget.iOS);
            UpdateBuildConfig(type);
            AssetDatabase.Refresh();

            bool debugUIEnabled = type != PackageType.Publish;
            GameBuilderProcess.ProcessSymbols(BuildTarget.iOS, debugUIEnabled);
            GameBuilderProcess.ProcessStreamingAssets(BuildTarget.iOS);

            var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, iosProjectDirectory, BuildTarget.iOS, BuildOptions.None);

            GameBuilderProcess.RevertSymbolsModification(BuildTarget.iOS);
            GameBuilderProcess.RevertStreamingAssetsModification();
            RestoreBuildConfig();

            AssetDatabase.Refresh();
            if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                MergeIOSProject();
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void PrepareDirectory(BuildTarget platform)
        {
            string output = Path.Combine(Path.GetFullPath("."), OutputDirectory);
            if (Directory.Exists(output) == false)
            {
                Directory.CreateDirectory(output);
            }
            if (platform == BuildTarget.StandaloneWindows64)
            {
                windowsFilePath = Path.Combine(output, PlatformWindows);
                if (Directory.Exists(windowsFilePath))
                {   
                    Directory.Delete(windowsFilePath, true);
                }
                Directory.CreateDirectory(windowsFilePath);
                //if (LoginSystem.channel == D7.Pb.Channel.Kuaishow)
                //{
                //    //Windows平台BuildPlayer的locationPathName是输出的exe完整文件路径，Android与iOS平台则是一个目录，注意区别
                //    windowsFilePath = Path.Combine(windowsFilePath, "ks700309743548758981.exe");
                //}
            }
            else if (platform == BuildTarget.Android)
            {
                string androidPath = Path.Combine(output, PlatformAndroid);

                if (Directory.Exists(androidPath) == false)
                {
                    Directory.CreateDirectory(androidPath);
                }

                androidProjectDirectory = Path.Combine(androidPath, PlayerSettings.productName);
                if (Directory.Exists(androidProjectDirectory) == false)
                {
                    Directory.CreateDirectory(androidProjectDirectory);
                }
                else
                {
                    Directory.Delete(androidProjectDirectory, true);
                }
            }
            else if (platform == BuildTarget.iOS)
            {
                string iosPath = Path.Combine(output, PlatformIOS);
                if (Directory.Exists(iosPath) == false)
                {
                    Directory.CreateDirectory(iosPath);
                }

                iosProjectDirectory = Path.Combine(iosPath, PlayerSettings.productName);
                if (Directory.Exists(iosProjectDirectory) == false)
                {
                    Directory.CreateDirectory(iosProjectDirectory);
                }
                else
                {
                    Directory.Delete(iosProjectDirectory, true);
                }
            }
        }

        private static void UpdateBuildConfig(PackageType type)
        {
            Debug.Log($"BuildAndroidProject PackageType: {type}");
            //将工程中的配置拷贝出去，待出包完成之后还原
            string sourceFile = Path.Combine(Path.GetFullPath("."), BuildConfig.ConfigFileAssetPath);
            string targetFile = Path.Combine(Path.GetFullPath("."), OutputDirectory, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, targetFile, true);

            //往工程中写入新的配置
            BuildConfigData config = new BuildConfigData();
            config.packageType = type;
            config.buildTimestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            string content = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(sourceFile, content);

            //生成新的VersionCode(年份、天数、时分)
            StringBuilder targetConfig = new StringBuilder();
            string versionCode = $"{DateTime.Now:yy}{DateTime.Now.DayOfYear}{DateTime.Now:HHmm}";
            //string versionCode = $"24821202";//测试使用
            string targetType = type.ToString();
            targetConfig.AppendLine(PlayerSettings.productName);
            targetConfig.AppendLine(versionCode);
            targetConfig.Append(targetType);
            string path = Path.Combine(Path.GetFullPath("."), OutputDirectory, Path.GetFileNameWithoutExtension(BuildConfig.ConfigFileAssetPath) + ".txt");
            File.WriteAllText(path, targetConfig.ToString());
        }

        private static void RestoreBuildConfig()
        {
            string sourceFile = Path.Combine(Path.GetFullPath("."), OutputDirectory, Path.GetFileName(BuildConfig.ConfigFileAssetPath));
            string targetFile = Path.Combine(Path.GetFullPath("."), BuildConfig.ConfigFileAssetPath);
            File.Copy(sourceFile, targetFile, true);
        }

        #region Unity命令行调用接口

        private static void InitUnityProject()
        {
            string productName = GetCommandLineArgs("-productName");
            PlayerSettings.productName = productName;
        }

        private static void ExportAndroidProject()
        {
            string packageType = GetCommandLineArgs("-packageType");
            string content = $"{packageType.Substring(0, 1).ToUpper()}{packageType.Substring(1)}";
            PackageType type = (PackageType)Enum.Parse(typeof(PackageType), content);
            ExportAndroidProject(type);
        }

        private static void ExportIOSProject()
        {
            string packageType = GetCommandLineArgs("-packageType");
            string content = $"{packageType.Substring(0, 1).ToUpper()}{packageType.Substring(1)}";
            PackageType type = (PackageType)Enum.Parse(typeof(PackageType), content);
            ExportIOSProject(type);
        }

        /// <summary>
        /// 获取命令行参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetCommandLineArgs(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && args.Length > i + 1)
                {
                    Debug.Log($"Command Args---->{args[i]}:{args[i + 1]}");
                    return args[i + 1];
                }
            }
            return null;
        }

        #endregion
    }
}