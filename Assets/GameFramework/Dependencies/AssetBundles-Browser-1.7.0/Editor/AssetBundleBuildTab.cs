using AssetBundleBrowser.AssetBundleDataSource;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    [System.Serializable]
    public class AssetBundleBuildTab
    {
        private const string Server_Host_File = "../../pipeline/asset_server_host.txt";
        private const string Server_Version_File = "../../pipeline/asset_server_version.txt";
        [SerializeField]
        private Vector2 m_ScrollPosition;
        private AssetBundleInspectTab m_InspectTab;
        [SerializeField]
        private BuildTabData m_UserData;

        GUIContent m_buildTarget;
        GUIContent m_compression;
        GUIContent m_forceRebuild;
        GUIContent m_copy;
        GUIContent m_upload;
        GUIContent m_serverHost;
        GUIContent m_serverVersion;

        private List<string> host;
        private List<string> version;

        internal enum CompressOptions
        {
            Uncompressed = 0,
            StandardCompression,
            ChunkBasedCompression,
        }

        //Note: this is the provided BuildTarget enum with some entries removed as they are invalid in the dropdown
        internal enum ValidBuildTarget
        {
            //NoTarget = -2,        --doesn't make sense
            //iPhone = -1,          --deprecated
            //BB10 = -1,            --deprecated
            //MetroPlayer = -1,     --deprecated
            StandaloneOSXUniversal = 2,
            StandaloneOSXIntel = 4,
            StandaloneWindows = 5,
            WebPlayer = 6,
            WebPlayerStreamed = 7,
            iOS = 9,
            PS3 = 10,
            XBOX360 = 11,
            Android = 13,
            StandaloneLinux = 17,
            StandaloneWindows64 = 19,
            WebGL = 20,
            WSAPlayer = 21,
            StandaloneLinux64 = 24,
            StandaloneLinuxUniversal = 25,
            WP8Player = 26,
            StandaloneOSXIntel64 = 27,
            BlackBerry = 28,
            Tizen = 29,
            PSP2 = 30,
            PS4 = 31,
            PSM = 32,
            XboxOne = 33,
            SamsungTV = 34,
            N3DS = 35,
            WiiU = 36,
            tvOS = 37,
            Switch = 38
        }

        internal AssetBundleBuildTab()
        {
            m_UserData = new BuildTabData();
        }

        internal void OnEnable(EditorWindow parent)
        {
            m_InspectTab = (parent as AssetBundleBrowserMain).m_InspectTab;

            //LoadData...
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserBuild.dat";

            if (File.Exists(dataPath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(dataPath, FileMode.Open);
                if (bf.Deserialize(file) is BuildTabData data)
                    m_UserData = data;
                file.Close();
            }

            m_buildTarget = new GUIContent("Build Target", "构建资源对应的目标平台");
            m_compression = new GUIContent("Compression", "构建资源的压缩格式");
            m_forceRebuild = new GUIContent("Force Rebuild", "是否强制重新构建");
            m_upload = new GUIContent("Upload Server", "是否上传到指定服务器");
            m_copy = new GUIContent("Copy", "是否拷贝到指定路径");
            m_serverHost = new GUIContent("Server Host", "服务器IP");
            m_serverVersion = new GUIContent("Server Version", "服务器版本");

            string hostPath = Path.Combine(Path.GetFullPath("."), Server_Host_File);
            string versionPath = Path.Combine(Path.GetFullPath("."), Server_Version_File);
            if (File.Exists(hostPath) && File.Exists(versionPath))
            {
                string[] serverHost = File.ReadAllLines(hostPath);
                string[] serverVersion = File.ReadAllLines(versionPath);
                if (host == null)
                {
                    host = new List<string>(serverHost.Length);
                    version = new List<string>(serverVersion.Length);
                }
                host.Clear();
                version.Clear();

                foreach (var item in serverHost)
                {
                    host.Add(item);
                }

                foreach (var item in serverVersion)
                {
                    version.Add(item);
                }
            }
        }

        internal void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            EditorGUILayout.Space();
            GUILayout.BeginVertical();

            //build target
            ValidBuildTarget target = (ValidBuildTarget)EditorGUILayout.EnumPopup(m_buildTarget, m_UserData.m_BuildTarget);
            if (target != m_UserData.m_BuildTarget)
            {
                m_UserData.m_BuildTarget = target;
            }
            EditorGUILayout.Space();

            //compression
            CompressOptions options = (CompressOptions)EditorGUILayout.EnumPopup(m_compression, m_UserData.m_Compression);
            if (options != m_UserData.m_Compression)
            {
                m_UserData.m_Compression = options;
            }
            EditorGUILayout.Space();

            //forceRebuild
            bool forceState = EditorGUILayout.ToggleLeft(m_forceRebuild, m_UserData.m_forceRebuild);
            if (forceState != m_UserData.m_forceRebuild)
            {
                m_UserData.m_forceRebuild = forceState;
            }
            EditorGUILayout.Space();

            //copy
            bool copyState = EditorGUILayout.ToggleLeft(m_copy, m_UserData.m_copy);
            if (copyState != m_UserData.m_copy)
            {
                m_UserData.m_copy = copyState;
            }
            if (copyState)
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                var newPath = EditorGUILayout.TextField("Copy Path", m_UserData.m_copyPath);
                if (string.IsNullOrEmpty(newPath) == false && newPath != m_UserData.m_copyPath)
                {
                    m_UserData.m_copyPath = newPath;
                }
                if (GUILayout.Button("Browse", GUILayout.MaxWidth(75f)))
                {
                    BrowseForFolder();
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();

            //upload
            bool uploadState = EditorGUILayout.ToggleLeft(m_upload, m_UserData.m_upload);
            if (uploadState != m_UserData.m_upload)
            {
                m_UserData.m_upload = uploadState;
            }
            if (uploadState)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();

                if (host?.Count > 0 && version?.Count > 0)
                {
                    int selectedHostIndex = 0;
                    GUIContent[] hostContent = new GUIContent[host.Count];
                    for (int i = 0; i < host.Count; i++)
                    {
                        hostContent[i] = new GUIContent(host[i]);
                        if (string.IsNullOrEmpty(m_UserData.m_serverHost) == false)
                        {
                            if (host[i].Contains(m_UserData.m_serverHost))
                            {
                                selectedHostIndex = i;
                            }
                        }
                    }

                    int hostIndex = EditorGUILayout.Popup(m_serverHost, selectedHostIndex, hostContent);
                    string serverHost = host[hostIndex].Split(',')[1];
                    m_UserData.m_serverHost = serverHost;
                    EditorGUILayout.Space();

                    int selectedVersionIndex = 0;
                    GUIContent[] versionContent = new GUIContent[version.Count];
                    for (int i = 0; i < version.Count; i++)
                    {
                        versionContent[i] = new GUIContent(version[i]);
                        if (string.IsNullOrEmpty(m_UserData.m_serverVersion) == false)
                        {
                            if (version[i].Contains(m_UserData.m_serverVersion))
                            {
                                selectedVersionIndex = i;
                            }
                        }
                    }
                    int versionIndex = EditorGUILayout.Popup(m_serverVersion, selectedVersionIndex, versionContent);
                    string serverVersion = version[versionIndex].Split(',')[1];
                    m_UserData.m_serverVersion = serverVersion;
                }
                else
                {
                    var content = EditorGUIUtility.IconContent("CollabConflict Icon");
                    content.text = "请在 pipeline 目录创建 asset_server_host.txt 与 asset_server_version.txt 文件\n文件内容请参考 asset_server_host_template.txt 与 asset_server_version_template.txt";
                    GUILayout.Label(content);
                }
                GUILayout.EndHorizontal();
            }

            //build
            EditorGUILayout.Space();
            if (GUILayout.Button("Build"))
            {
                EditorApplication.delayCall += ExecuteBuild;
            }
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void ExecuteBuild()
        {
            string outPutPath = Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, m_UserData.m_BuildTarget.ToString(), "AssetBundle"));
            BuildAssetBundleOptions options = BuildAssetBundleOptions.None;
            if (m_UserData.m_Compression == CompressOptions.Uncompressed)
            {
                options |= BuildAssetBundleOptions.UncompressedAssetBundle;
            }
            else if (true)
            {
                options |= BuildAssetBundleOptions.ChunkBasedCompression;
            }
            string address = m_UserData.m_serverHost.Replace('\\', '/') + m_UserData.m_serverVersion.Replace('\\', '/');

            ABBuildInfo info = new ABBuildInfo
            {
                outputDirectory = outPutPath,
                options = options,
                buildTarget = (BuildTarget)m_UserData.m_BuildTarget,
                onBuild = (assetBundleName) =>
                {
                    if (m_InspectTab == null)
                        return;
                    m_InspectTab.AddBundleFolder(outPutPath);
                    m_InspectTab.RefreshBundles();
                },
                copy = m_UserData.m_copy,
                copyPath = m_UserData.m_copyPath,
                upload = m_UserData.m_upload,
                uploadAddress = address,
            };
            BuildAssetBundleMgr.BuildAssetBundle(info);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private void BrowseForFolder()
        {
            var newPath = EditorUtility.OpenFolderPanel("选择拷贝路径", m_UserData.m_copyPath, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
            {
                var gamePath = Path.GetFullPath(".");
                gamePath = gamePath.Replace("\\", "/");
                if (newPath.StartsWith(gamePath) && newPath.Length > gamePath.Length)
                    newPath = newPath.Remove(0, gamePath.Length + 1);
                m_UserData.m_copyPath = newPath;
            }
        }

        internal void OnDisable()
        {
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserBuild.dat";

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream file = File.Create(dataPath))
            {
                bf.Serialize(file, m_UserData);
            }
        }

        [System.Serializable]
        internal class BuildTabData
        {
            internal ValidBuildTarget m_BuildTarget = ValidBuildTarget.Android;
            internal CompressOptions m_Compression = CompressOptions.Uncompressed;
            internal bool m_forceRebuild = false;
            internal bool m_copy = false;
            internal string m_copyPath = "";
            internal bool m_upload = false;
            internal string m_serverHost = "";
            internal string m_serverVersion = "";
        }
    }
}