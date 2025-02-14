using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GameFramework
{
    public class ResourcesConfigGenerator : AssetPostprocessor
    {
        public static bool Enabled = true;

        private const string configFileName = "ResourcesConfig.txt";
        private static Regex assetPathRegex = new Regex(@"(.+?)Resources\/((.+?\/)*)(.+?\..+)");

        private static HashSet<string> ignoreExtension = new HashSet<string>(){ ".cginc", ".hlsl" };

        private static bool isResourcesDirty = false;
        private static readonly Stopwatch stopwatch = new Stopwatch();
        /// <summary>
        /// 外层的字典Key是资源名
        /// 里层的字典Key是资源类型
        /// 里层的字典Value是键值对：Key是资源路径，Value是文件扩展名
        /// </summary>
        private static Dictionary<string, Dictionary<string, KeyValuePair<string, string>>> assetNameHashDict = new Dictionary<string, Dictionary<string, KeyValuePair<string, string>>>();
        private static HashSet<string> removedFile = new HashSet<string>();

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Enabled == false)
            {
                EditorApplication.update -= EditorUpdate;
                stopwatch.Reset();
                return;
            }
            removedFile.Clear();

            if (isResourcesDirty == false)
            {
                string configFilePath = Path.GetFullPath($"Assets/Resources/{configFileName}");
                string[] configFileContent = null;
                if (assetNameHashDict.Count == 0)
                {
                    if (File.Exists(configFilePath))
                    {
                        configFileContent = File.ReadAllLines(configFilePath);
                        foreach (string line in configFileContent)
                        {
                            var splits = line.Split('|');
                            string name = splits[0];
                            string type = splits[1];
                            string path = splits[2];
                            string extension = splits[3];

                            if (assetNameHashDict.TryGetValue(name, out var typeDict) == false)
                            {
                                typeDict = new Dictionary<string, KeyValuePair<string, string>>();
                                assetNameHashDict.Add(name, typeDict);
                            }
                            typeDict.Add(type, new KeyValuePair<string, string>(path, extension));
                        }
                    }
                }

                foreach (string assetPath in deletedAssets)
                {
                    if (assetPath.EndsWith(configFileName))
                    {
                        continue;
                    }

                    string fileExtension = Path.GetExtension(assetPath);
                    if (ignoreExtension.Contains(fileExtension))
                    {
                        continue;
                    }
                    string fileName = Path.GetFileName(assetPath);
                    removedFile.Add(fileName);
                    if (assetPath.Contains("/Resources/"))
                    {
                        isResourcesDirty = true;
                        break;
                    }
                }
            }

            if (isResourcesDirty == false)
            {
                foreach (string assetPath in movedFromAssetPaths)
                {
                    if (assetPath.EndsWith(configFileName))
                    {
                        continue;
                    }

                    string fileExtension = Path.GetExtension(assetPath);
                    if (ignoreExtension.Contains(fileExtension))
                    {
                        continue;
                    }
                    string fileName = Path.GetFileName(assetPath);
                    removedFile.Add(fileName);
                    if (assetPath.Contains("/Resources/"))
                    {
                        isResourcesDirty = true;
                        break;
                    }
                }

                foreach (string assetPath in movedAssets)
                {
                    if (File.Exists(assetPath) == false)
                    {
                        continue;
                    }
                    if (assetPath.EndsWith(configFileName))
                    {
                        continue;
                    }

                    string fileExtension = Path.GetExtension(assetPath);
                    if (ignoreExtension.Contains(fileExtension))
                    {
                        continue;
                    }
                    string fileName = Path.GetFileName(assetPath);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);
                    string filePathWithoutName = Path.GetDirectoryName(assetPath).Replace("\\", "/").Replace("Assets/Resources", "").Trim('/');
                    string fileType = AssetImporter.GetAtPath(assetPath).GetType().Name;
                    if (removedFile.Contains(fileName) == false)
                    {
                        if (assetNameHashDict.TryGetValue(fileNameWithoutExtension, out var typeDict))
                        {
                            if (typeDict.TryGetValue(fileType, out var kvp))
                            {
                                if (filePathWithoutName != kvp.Key || fileExtension != kvp.Value)
                                {
                                    Debug.LogError($"检测到重复的资源名称 {filePathWithoutName}/{fileNameWithoutExtension}{fileExtension} <===> {kvp.Key}/{fileNameWithoutExtension}{kvp.Value}");
                                }
                                continue;
                            }
                        }
                    }
                    if (assetPath.Contains("/Resources/"))
                    {
                        isResourcesDirty = true;
                        break;
                    }
                }
            }

            if (isResourcesDirty == false)
            {
                foreach (string assetPath in importedAssets)
                {
                    if (File.Exists(assetPath) == false)
                    {
                        continue;
                    }

                    if (assetPath.Contains("/Resources/"))
                    {
                        if (assetPath.EndsWith(configFileName))
                        {
                            continue;
                        }

                        string fileExtension = Path.GetExtension(assetPath);
                        if (ignoreExtension.Contains(fileExtension))
                        {
                            continue;
                        }

                        string fileName = Path.GetFileName(assetPath);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assetPath);
                        string filePathWithoutName = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                        filePathWithoutName = filePathWithoutName.Substring(filePathWithoutName.IndexOf("/Resources") + "/Resources".Length).Trim('/');
                        string fileType = AssetDatabase.LoadMainAssetAtPath(assetPath).GetType().Name;
                        if (removedFile.Contains(fileName) == false)
                        {
                            if (assetNameHashDict.TryGetValue(fileNameWithoutExtension, out var typeDict))
                            {
                                if (typeDict.TryGetValue(fileType, out var kvp))
                                {
                                    if (filePathWithoutName != kvp.Key || fileExtension != kvp.Value)
                                    {
                                        Debug.LogError($"检测到重复的资源名称 {filePathWithoutName}/{fileNameWithoutExtension}{fileExtension} <===> {kvp.Key}/{fileNameWithoutExtension}{kvp.Value}");
                                    }
                                    continue;
                                }
                            }
                        }

                        isResourcesDirty = true;
                        break;
                    }
                }
            }

            if (isResourcesDirty)
            {
                Debug.Log("检测到Resources目录变化，重置计时器！");
                EditorApplication.update += EditorUpdate;
                stopwatch.Restart();
            }
        }

        private static void EditorUpdate()
        {
            if (stopwatch.ElapsedMilliseconds > GameEditorConfig.ResourceConfig.autoGenerateDelay * 1000)
            {
                EditorApplication.update -= EditorUpdate;
                stopwatch.Reset();
                if (isResourcesDirty && Enabled)
                {
                    Debug.LogFormat("延迟时间{0}秒到，更新ResourcesConfig文件！", GameEditorConfig.ResourceConfig.autoGenerateDelay);
                    isResourcesDirty = false;
                    GenerateResourcesConfig();
                }
            }
        }

        public static void GenerateResourcesConfig()
        {
            stopwatch.Start();
            string resourcesPath = $"{Application.dataPath}/Resources";
            DirectoryInfo resourcesDir = new DirectoryInfo(resourcesPath);
            if (resourcesDir.Exists == false)
            {
                return;
            }

            var allAssets = Resources.LoadAll("");

            assetNameHashDict.Clear();

            string fileContent = Path.Combine(resourcesPath, configFileName);
            StringBuilder sb = new StringBuilder(1000);
            int index = 0;
            foreach (var asset in allAssets)
            {
                string fileName = asset.name;
                if (fileName.StartsWith("Skeleton Prefab Mesh"))
                {
                    continue;
                }
                if (asset is Sprite sprite)
                {
                    if (sprite.texture.name != fileName)
                    {
                        //如果Sprite的名字和其所属的图片名字不一样，大概率这是一个图集，这种Sprite没必要记录，因为没法通过AssetSystem加载
                        continue;
                    }
                }
                string fileType = asset.GetType().Name;
                if (fileName.Equals(configFileName))
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(asset);
                var match = assetPathRegex.Match(assetPath);
                string assetFolderPath = match.Groups[1].Value;
                if (assetFolderPath.Equals("Assets/") == false && assetFolderPath.Equals("Assets/GameFramework/") == false)
                {
                    continue;
                }
                string fileExtension = Path.GetExtension(assetPath);
                if (ignoreExtension.Contains(fileExtension))
                {
                    continue;
                }

                string filePathWithoutName = match.Groups[2].Value;
                if (filePathWithoutName.EndsWith('/'))
                {
                    filePathWithoutName = filePathWithoutName.Substring(0, filePathWithoutName.Length - 1);
                }

                if (assetNameHashDict.TryGetValue(fileName, out var typeDict) == false)
                {
                    typeDict = new Dictionary<string, KeyValuePair<string, string>>();
                    typeDict.Add(fileType, new KeyValuePair<string, string>(filePathWithoutName, fileExtension));
                    assetNameHashDict.Add(fileName, typeDict);
                }
                if (typeDict.TryGetValue(fileType, out var kvp))
                {
                    if (filePathWithoutName != kvp.Key || fileExtension != kvp.Value)
                    {
                        Debug.LogError($"检测到重复的资源名称 {filePathWithoutName}/{fileName}{fileExtension} <===> {kvp.Key}/{fileName}{kvp.Value}");
                    }
                }
                sb.Append(fileName);
                sb.Append("|");
                sb.Append(fileType);
                sb.Append("|");
                sb.Append(filePathWithoutName);
                sb.Append("|");
                sb.Append(fileExtension);
                if (index < allAssets.Length - 1)
                {
                    sb.AppendLine();
                }
                index++;
            }

            File.WriteAllText(fileContent, sb.ToString());
            AssetDatabase.Refresh();
            stopwatch.Stop();
            Debug.LogFormat("ResourcesConfig更新完成，用时：{0:F2}秒", stopwatch.Elapsed.TotalSeconds);
        }
    }
}