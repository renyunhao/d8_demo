using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;

namespace GameFramework
{
    public partial class GameEditorMenu : EditorWindow
    {
        private bool dataGenerateFoldout = true;
        private bool buildFoldout = true;
        private bool shortcutFoldout = true;
        private bool clearCacheFoldout = true;

        private Vector2 scrollViewPos;

        [MenuItem("游戏/功能菜单")]
        public static void Init()
        {
            GameEditorMenu wnd = GetWindow<GameEditorMenu>();
            wnd.titleContent = new GUIContent("功能菜单");
        }

        [MenuItem("游戏/配置")]
        public static void Config()
        {
            GameEditorConfig config = GameEditorConfig.Check();
            Selection.activeObject = config;
        }

        private void OnGUI()
        {
            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos);
            dataGenerateFoldout = EditorGUILayout.Foldout(dataGenerateFoldout, "数据生成");
            if (dataGenerateFoldout)
            {
                if (GUILayout.Button("更新数据表"))
                {
                    DataTablePipeline pipeline = new DataTablePipeline();
                    pipeline.UpdateExcelToJson();
                }

                if (GUILayout.Button("更新文本表"))
                {
                    TextTablePipeline pipeline = new TextTablePipeline();
                    pipeline.UpdateExcelToJson();
                }

                if (GUILayout.Button("更新图集"))
                {
                    UpdateUIAtlas();
                }

                if (GUILayout.Button("生成JsonFormater"))
                {
                    Utf8JsonEditor.GenerateJsonFormatter();
                }

                if (GUILayout.Button("截屏"))
                {
                    string name = DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss");
                    string fileName = $"{name}.png";
                    ScreenCapture.CaptureScreenshot(fileName);
                    Debug.Log($"截屏成功，文件保存在：{Path.GetFullPath(fileName)}");
                }

                if (GUILayout.Button("提取文本表所有字符"))
                {
                    ExtractTextDataTableCharacters();
                }

                ProjectCustomOnGUI();
            }

            buildFoldout = EditorGUILayout.Foldout(buildFoldout, "打包出版");
            if (buildFoldout)
            {
                //ProcessObfuscator.Enabled = GUILayout.Toggle(ProcessObfuscator.Enabled, "开启混淆");

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                if (GUILayout.Button("Windows 开发包"))
                {
                    GameBuilder.BuildWindows(PackageType.Develop);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Windows 发布包"))
                {
                    GameBuilder.BuildWindows(PackageType.Publish);
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                if (GUILayout.Button("导出Android工程"))
                {
                    GameBuilder.ExportAndroidProjectByCurrentConfig();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Android 开发包"))
                {
                    GameBuilder.BuildAPK(PackageType.Develop);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Android 发布包"))
                {
                    GameBuilder.BuildAPK(PackageType.Publish);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Android 测试包"))
                {
                    GameBuilder.BuildAPK(PackageType.Internal);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Android 体验包"))
                {
                    GameBuilder.BuildAPK(PackageType.External);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("合并Android工程"))
                {
                    GameBuilder.MergeAndroidProject();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("构建Android工程"))
                {
                    GameBuilder.BuildAndroidProject();
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                if (GUILayout.Button("导出iOS工程"))
                {
                    GameBuilder.ExportIOSProjectByCurrentConfig();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("iOS 开发包"))
                {
                    GameBuilder.BuildIPA(PackageType.Develop);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("iOS 发布包"))
                {
                    GameBuilder.BuildIPA(PackageType.Publish);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("iOS 测试包"))
                {
                    GameBuilder.BuildIPA(PackageType.Internal);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("iOS 体验包"))
                {
                    GameBuilder.BuildIPA(PackageType.External);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("合并iOS工程"))
                {
                    GameBuilder.MergeIOSProject();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("构建iOS工程"))
                {
                    GameBuilder.BuildIOSProject();
                    GUIUtility.ExitGUI();
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            shortcutFoldout = EditorGUILayout.Foldout(shortcutFoldout, "快捷方式");
            if (shortcutFoldout)
            {
                if (GUILayout.Button("打开PersistentDataPath"))
                {
#if UNITY_EDITOR_OSX
                    string path = string.Format("\"{0}\"", Application.persistentDataPath);
                    System.Diagnostics.Process.Start("open", path);
#else
                    Application.OpenURL(Application.persistentDataPath);
#endif
                }

                if (GUILayout.Button("打开StreamingAssetsPath"))
                {
#if UNITY_EDITOR_OSX
                    string path = string.Format("\"{0}\"", Application.streamingAssetsPath);
                    System.Diagnostics.Process.Start("open", path);
#else
                    Application.OpenURL(Application.streamingAssetsPath);
#endif
                }

                if (GUILayout.Button("打开Editor Log"))
                {
#if UNITY_EDITOR_OSX
                    string path = "\"~/Library/Logs/Unity\"";
                    System.Diagnostics.Process.Start("open", path);
#else
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/Unity/Editor";
                    Application.OpenURL(path);
#endif
                }
            }

            clearCacheFoldout = EditorGUILayout.Foldout(clearCacheFoldout, "存储清理");
            if (clearCacheFoldout)
            {
                if (GUILayout.Button("清除PersistentDataPath"))
                {
                    if (Directory.Exists(Application.persistentDataPath))
                    {
                        Directory.Delete(Application.persistentDataPath, true);
                    }
                    Debug.Log("清除PersistentDataPath成功");
                }

                if (GUILayout.Button("清除PlayerPrefs"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Debug.Log("清除PlayerPrefs成功");
                }

                if (GUILayout.Button("清除Cache目录"))
                {
                    string cacheFolder = Application.persistentDataPath + "/Cache";
                    if (Directory.Exists(cacheFolder))
                    {
                        Directory.Delete(cacheFolder, true);
                    }
                    Debug.Log("清除Cache目录成功");
                }

                if (GUILayout.Button("清除AssetBundle"))
                {
                    try
                    {
                        string assetBundleFolder = Application.persistentDataPath + "/AssetBundle";
                        if (Directory.Exists(assetBundleFolder))
                        {
                            Directory.Delete(assetBundleFolder, true);
                        }
                        EditorUtility.DisplayDialog("清除AssetBundles", "完成", "OK");
                    }
                    catch (System.Exception ex)
                    {
                        EditorUtility.DisplayDialog("清除AssetBundles", "出现错误，请手动处理或呼叫客户端：\n" + ex.ToString(), "OK");
                    }
                }
            }

            EditorGUILayout.Foldout(true, "快捷替换");
            if (GUILayout.Button("名称替换"))
            {
                SetItemName();
                Debug.Log("名称替换成功");
            }

            EditorGUILayout.EndScrollView();
        }

        private void ExtractTextDataTableCharacters()
        {
            var text = Resources.Load<TextAsset>("TextData/TextTableData").text;
            var hashset = new HashSet<char>();
            foreach (var c in text)
            {
                hashset.Add(c);
            }
            var sb = new System.Text.StringBuilder(10000);
            foreach (var c in hashset)
            {
                sb.Append(c);
            }
            string outputFilePath = Application.dataPath + "/../../../output/AllCharacter.txt";
            File.WriteAllText(outputFilePath, sb.ToString());
            Debug.Log("文本表所有字符（去重）已经导出到文件：" + outputFilePath);
        }

        partial void ProjectCustomOnGUI();

        [MenuItem("Assets/复制资源路径")]
        public static void CopyTheResourcePath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
            path = path.Replace("Assets/Resources/", "");
            GUIUtility.systemCopyBuffer = path;
        }

        public static void UpdateUIAtlas()
        {
            string fullPath = Path.Combine(Application.dataPath, GameEditorConfig.AtlasConfig.spritePath);
            if (Directory.Exists(fullPath))
            {
                var files = Directory.GetFiles(fullPath).Where(x => !x.EndsWith(".meta"));
                if (files.Count() > 0)
                {
                    Debug.LogError($"根目录不允许放文件，只能放文件夹：{fullPath}");
                    return;
                }

                var folders = Directory.GetDirectories(fullPath);
                foreach (var folder in folders)
                {
                    var folderName = Path.GetFileName(folder);
                    var atlasFilePath = Path.Combine(Application.dataPath, GameEditorConfig.AtlasConfig.atlasPath, folderName) + ".spriteatlasv2";
                    var atlasAssetPath = $"Assets/{GameEditorConfig.AtlasConfig.atlasPath}/{folderName}.spriteatlasv2";

                    var spritesFolderAssetPath = $"Assets/{GameEditorConfig.AtlasConfig.spritePath}/{folderName}";
                    var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(spritesFolderAssetPath);
                    SpriteAtlasAsset atlas = new SpriteAtlasAsset();
                    atlas.Add(new UnityEngine.Object[] { folderAsset });
                    SpriteAtlasAsset.Save(atlas, atlasAssetPath);
                }
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError($"散图目录不存在：{fullPath}");
            }
        }

        public static void SetItemName()
        {
            var guids = AssetDatabase.FindAssets("", new[] { "Assets/ResourcesRaw/Sprite/EquipUI" });
            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                AssetDatabase.RenameAsset(assetPath, ZString.Format("Item_{0}", asset.name));
                //asset.name = ZString.Format("Item_{0}", asset.name);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static GameObject FindInActiveParent(GameObject go)
        {
            GameObject inactiveParent = go;
            Transform parent = go.transform;
            while (parent != null && parent.gameObject.activeInHierarchy == false)
            {
                inactiveParent = parent.gameObject;
                parent = parent.parent;
            }
            return inactiveParent;
        }

        public static TextAlignmentOptions ToTMPTextAnchor(TextAnchor textAnchor)
        {
            if (textAnchor == TextAnchor.UpperLeft)
            {
                return TextAlignmentOptions.TopLeft;
            }
            else if (textAnchor == TextAnchor.UpperCenter)
            {
                return TextAlignmentOptions.Top;
            }
            else if (textAnchor == TextAnchor.UpperRight)
            {
                return TextAlignmentOptions.TopRight;
            }
            else if (textAnchor == TextAnchor.MiddleLeft)
            {
                return TextAlignmentOptions.Left;
            }
            else if (textAnchor == TextAnchor.MiddleCenter)
            {
                return TextAlignmentOptions.Center;
            }
            else if (textAnchor == TextAnchor.MiddleRight)
            {
                return TextAlignmentOptions.Right;
            }
            else if (textAnchor == TextAnchor.LowerLeft)
            {
                return TextAlignmentOptions.BottomLeft;
            }
            else if (textAnchor == TextAnchor.LowerCenter)
            {
                return TextAlignmentOptions.Bottom;
            }
            else if (textAnchor == TextAnchor.LowerRight)
            {
                return TextAlignmentOptions.BottomRight;
            }
            return TextAlignmentOptions.Center;
        }
    }
}