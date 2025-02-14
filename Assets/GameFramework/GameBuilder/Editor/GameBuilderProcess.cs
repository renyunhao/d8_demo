using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameFramework
{
    public class GameBuilderProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public const string SYMBOL_GM = "GM";
        public const string SYMBOL_GM_WITH_SEMICOLON = ";GM";

        private static string symbolsBeforeBuild;
        private static string[] moveOutFolders;

        public int callbackOrder
        {
            get
            {
                return 0;
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            ResourcesConfigGenerator.Enabled = false;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            RevertSymbolsModification(report.summary.platform);
            RevertStreamingAssetsModification();
            AssetDatabase.Refresh();
            ResourcesConfigGenerator.Enabled = true;
        }

        public static BuildTargetGroup BuildTargetToGroup(BuildTarget target)
        {
            if (target == BuildTarget.Android)
            {
                return BuildTargetGroup.Android;
            }
            else if (target == BuildTarget.iOS)
            {
                return BuildTargetGroup.iOS;
            }
            else if (target == BuildTarget.StandaloneWindows64)
            {
                return BuildTargetGroup.Standalone;
            }
            return BuildTargetGroup.Unknown;
        }

        /// <summary>
        /// 确定最终的Script Define Symbols，目前用于去掉DebugUI中的调试功能，避免被破解者利用
        /// </summary>
        /// <param name="target"></param>
        public static void ProcessSymbols(BuildTarget target, bool debugUIEnabled)
        {
            BuildTargetGroup targetGroup = BuildTargetToGroup(target);
            symbolsBeforeBuild = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            if (debugUIEnabled)
            {
                if (symbolsBeforeBuild.Contains(SYMBOL_GM) == false)
                {
                    string newSymbols;
                    if (string.IsNullOrEmpty(symbolsBeforeBuild))
                    {
                        newSymbols = SYMBOL_GM;
                    }
                    else
                    {
                        newSymbols = symbolsBeforeBuild + SYMBOL_GM_WITH_SEMICOLON;
                    }

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newSymbols);
                }
            }
            else
            {
                string newSymbols;
                if (symbolsBeforeBuild.Contains(SYMBOL_GM_WITH_SEMICOLON))
                {
                    newSymbols = symbolsBeforeBuild.Replace(SYMBOL_GM_WITH_SEMICOLON, "");
                }
                else
                {
                    newSymbols = symbolsBeforeBuild.Replace(SYMBOL_GM, "");
                }
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newSymbols);
            }
        }

        public static void ProcessStreamingAssets(BuildTarget target)
        {
            moveOutFolders = null;
            if (target == BuildTarget.StandaloneWindows64)
            {
                moveOutFolders = new string[] { "Win" };
            }
            else if (target == BuildTarget.Android)
            {
                moveOutFolders = new string[] { "iOS" };
            }
            else if (target == BuildTarget.iOS)
            {
                moveOutFolders = new string[] { "Android" };
            }

            foreach (string moveOutFolder in moveOutFolders)
            {
                string moveSourcePath = Path.Combine(Application.streamingAssetsPath, moveOutFolder);
                if (Directory.Exists(moveSourcePath))
                {
                    string moveDestPath = Path.Combine(Application.dataPath + "/../", moveOutFolder);
                    if (Directory.Exists(moveDestPath))
                    {
                        Directory.Delete(moveDestPath, true);
                    }

                    Directory.Move(moveSourcePath, moveDestPath);

                    File.Move(moveSourcePath + ".meta", moveDestPath + ".meta");
                }
            }
        }

        public static void RevertStreamingAssetsModification()
        {
            if (moveOutFolders == null)
            {
                return;
            }
            foreach (string moveOutFolder in moveOutFolders)
            {
                string moveSourcePath = Path.Combine(Application.dataPath + "/../", moveOutFolder);
                if (Directory.Exists(moveSourcePath))
                {
                    string moveDestPath = Path.Combine(Application.streamingAssetsPath, moveOutFolder);
                    Directory.Move(moveSourcePath, moveDestPath);

                    File.Move(moveSourcePath + ".meta", moveDestPath + ".meta");
                }
            }
        }

        /// <summary>
        /// 还原symbols
        /// </summary>
        /// <param name="target"></param>
        public static void RevertSymbolsModification(BuildTarget target)
        {
            BuildTargetGroup targetGroup = BuildTargetToGroup(target);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbolsBeforeBuild);
        }
    }
}