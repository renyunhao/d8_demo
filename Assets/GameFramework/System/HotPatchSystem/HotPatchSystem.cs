using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO.Compression;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace GameFramework
{
    public struct UpdateFileInfo
    {
        public string fileName;
        public string fileHash;
        public long fileSize;
    }

    /// <summary>
    /// 游戏资源更新管理类,负责更新游戏资源
    /// </summary>
    public static class HotPatchSystem
    {
        public enum Phase
        {
            None,
            CalculateLocalVersion,
            DownloadServerVersion,
            CompareVersion,
            DownloadAssetBundle,
            UpdateCompleted,
        }

        public static event Action<string> Event_DownloadVersionError;
        public static event Action Event_DownloadVersionFinished;
        public static event Action<long> Event_NeedPatch;
        public static event Action Event_DownloadStart;
        public static event Action Event_DownloadError;
        public static event Action Event_DownloadFinished;
        public static event Action Event_HotPatchFinished;

        public static Phase CurrentPhase { get; private set; }
        public static float CurrentProgress { get; private set; }

        public static readonly string Directory_AssetBundle = "AssetBundle/";
        public static readonly string File_Version = "versionInfo.txt";
        private static readonly List<string> uncompressedFile = new List<string>() { "AssetBundle", "assetBundleIni.txt" };

        /// <summary>
        /// 准备下载的文件信息
        /// </summary>
        private static Queue<UpdateFileInfo> willDownloadFiles = new Queue<UpdateFileInfo>(100);
        /// <summary>
        /// 下载出错的文件信息
        /// </summary>
        private static Queue<UpdateFileInfo> downloadedErrorFiles = new Queue<UpdateFileInfo>(100);
        /// <summary>
        /// 本地版本文件
        /// </summary>
        private static Dictionary<string, UpdateFileInfo> localVersion = new Dictionary<string, UpdateFileInfo>();

        private static string platform;
        private static string serverABDirectory;
        private static string localABDirectory;
        private static string localVersionPath;
        private static string serverVersionPath;
        private static string serverVersionInfo;
        private static UpdateFileInfo currentDownloadFile;
        private static UnityWebRequest currentWebRequest;
        private static int errorCount;

        public static long TotalSize { get; private set; }
        public static float DownloadedSize { get; private set; }

        private static Debug logger = new Debug("热更");

        #region Public Method

        /// <summary>
        /// 初始化资源更新模块
        /// </summary>
        public static void Initialize()
        {
            localABDirectory = Path.Combine(Application.persistentDataPath, Directory_AssetBundle);
            localVersionPath = Path.Combine(localABDirectory, File_Version);

            if (Application.platform == RuntimePlatform.Android
#if UNITY_EDITOR
            || EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
#else
            )
#endif
            {
                platform = "Android";
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer
#if UNITY_EDITOR
            || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
#else
            )
#endif
            {
                platform = "iOS";
            }
            else
            {
                platform = Application.platform.ToString();
                logger.E("未知平台，请在资源服务器上添加相应的平台目录:" + Application.platform);
            }
            logger.I("初始化资源更新模块");
        }

        /// <summary>
        /// 清理本地Assetbundle
        /// </summary>
        public static void ClearLocalAssetbundle()
        {
            if (Directory.Exists(localABDirectory))
            {
                Directory.Delete(localABDirectory, true);
            }
        }

        /// <summary>
        /// 检查AssetBundle更新,进入游戏时
        /// </summary>
        /// <param name="baseUrl"></param>
        public static void CheckToUpdate(string baseUrl)
        {
            Clear();
            if (string.IsNullOrEmpty(baseUrl))
            {
                logger.I($"热更地址为空,清除本地AssetBundle");
                if (Directory.Exists(localABDirectory))
                {
                    ClearLocalAssetbundle();
                }
                Event_HotPatchFinished?.Invoke();
            }
            else
            {
                logger.I($"开始检查热更:{baseUrl}");
                if (Directory.Exists(localABDirectory) == false)
                {
                    Directory.CreateDirectory(localABDirectory);
                }
                serverABDirectory = Path.Combine(baseUrl, platform, Directory_AssetBundle);
                serverVersionPath = Path.Combine(serverABDirectory, File_Version);
                CoroutineUtil.DoCoroutine(CalculateLocalFileVersion(), () =>
                {
                    CoroutineUtil.DoCoroutine(DownloadVersionFromServer());
                });
            }
        }

        public static void StartDownloadAssetBundles()
        {
            logger.I("开始下载热更文件");
            Event_DownloadStart?.Invoke();
            CoroutineUtil.DoCoroutine(DownloadAssetBundleZip(willDownloadFiles));
        }

        private static void UpdateAssetBundleFinish()
        {
            logger.I("没有热更文件需要更新");
            UpdateAssetBundleFinished(false);
        }

        #endregion

        #region Private Method

        /// <summary>
        /// 计算本地文件MD5值
        /// </summary>
        /// <returns></returns>
        private static IEnumerator CalculateLocalFileVersion()
        {
            localVersion.Clear();
            StringBuilder result = new StringBuilder();
            DirectoryInfo directoryInfo = new DirectoryInfo(localABDirectory);
            FileInfo[] fileInfos = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
            int count = 0;
            foreach (var item in fileInfos)
            {
                count++;
                string extension = Path.GetExtension(item.FullName);
                string nameWithExtension = Path.GetFileName(item.FullName);
                string md5 = string.Empty;
                if (nameWithExtension != File_Version && extension != ".zip")
                {
                    using (FileStream fileStream = new FileStream(item.FullName, FileMode.Open, FileAccess.Read))
                    {
                        System.Security.Cryptography.MD5 calculator = System.Security.Cryptography.MD5.Create();
                        byte[] buffer = calculator.ComputeHash(fileStream);
                        calculator.Clear();
                        //将字节数组转换成十六进制的字符串形式
                        StringBuilder stringBuilder = new StringBuilder();
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            stringBuilder.Append(buffer[i].ToString("x2"));
                        }
                        md5 = stringBuilder.ToString();
                    }
                    UpdateFileInfo info = new UpdateFileInfo
                    {
                        fileName = nameWithExtension,
                        fileHash = md5,
                        fileSize = item.Length
                    };
                    localVersion.Add(nameWithExtension, info);
                    result.AppendLine(string.Format("{0}|{1}|{2}", info.fileName, info.fileHash, info.fileSize));
                    if (count > 20)
                    {
                        count = 0;
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
            logger.I($"本地AssetBundleMD5值：\n" + result.ToString());
        }

        private static IEnumerator DownloadVersionFromServer()
        {
            logger.I("从服务器下载version文件,用以对比并得出需要下载的资源");
            CurrentPhase = Phase.DownloadServerVersion;
            using (currentWebRequest = UnityWebRequest.Get(serverVersionPath))
            {
                yield return currentWebRequest.SendWebRequest();
                if (string.IsNullOrEmpty(currentWebRequest.error))
                {
                    Event_DownloadVersionFinished?.Invoke();
                    if (currentWebRequest.responseCode == 200)
                    {
                        serverVersionInfo = currentWebRequest.downloadHandler.text;
                        logger.I("下载versionInfo.txt完成! 内容：\n" + serverVersionInfo);
                        if (string.IsNullOrEmpty(serverVersionInfo) == false)
                        {
                            string[] lines = serverVersionInfo.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                            Dictionary<string, UpdateFileInfo> sourceVersion = GetSourceFileVersionInfo(lines);
                            Dictionary<string, UpdateFileInfo> zipVersion = GetZipFileVersionInfo(lines);
                            CoroutineUtil.DoCoroutine(CompareVersion(sourceVersion, zipVersion));
                        }
                        else
                        {
                            logger.E("下载versionInfo.txt内容为空");
                        }
                    }
                    else
                    {
                        string content = $"下载文件{currentWebRequest.url}出现错误:{currentWebRequest.error}";
                        Event_DownloadVersionError?.Invoke(currentWebRequest.error);
                    }
                }
                else
                {
                    logger.E(string.Format("请求{0}出错：{1}", currentWebRequest.url, currentWebRequest.error));
                    string content = $"下载文件{currentWebRequest.url}出现错误:{currentWebRequest.error}";
                    Event_DownloadVersionError?.Invoke(currentWebRequest.error);
                }
            }
        }

        private static IEnumerator CompareVersion(Dictionary<string, UpdateFileInfo> sourceVersion, Dictionary<string, UpdateFileInfo> zipVersion)
        {
            logger.I("4.开始对比版本文件");
            TotalSize = 0;
            DownloadedSize = 0;
            UpdateFileInfo temp = new UpdateFileInfo();
            foreach (var serverPair in sourceVersion)
            {
                string fileName = serverPair.Key;
                UpdateFileInfo serverContent = serverPair.Value;
                if (localVersion.ContainsKey(fileName))
                {
                    UpdateFileInfo localContent = localVersion[fileName];
                    //文件大小不一致或者MD5不一致则需要更新
                    if (string.Equals(serverContent.fileHash, localContent.fileHash) == false ||
                        serverContent.fileSize != localContent.fileSize)
                    {
                        if (uncompressedFile.Contains(fileName) == false)
                        {
                            fileName += ".zip";
                        }
                        temp = zipVersion[fileName];
                        willDownloadFiles.Enqueue(temp);
                        TotalSize += temp.fileSize;
                        logger.I("需要更新文件：" + temp.fileName);
                    }
                }
                else
                {
                    if (uncompressedFile.Contains(fileName) == false)
                    {
                        fileName += ".zip";
                    }
                    temp = zipVersion[fileName];
                    willDownloadFiles.Enqueue(temp);
                    TotalSize += temp.fileSize;
                    logger.I("需要新增文件：" + temp.fileName);
                }
            }
            yield return new WaitForEndOfFrame();
            if (TotalSize > 0)
            {
                Event_NeedPatch?.Invoke(TotalSize);
            }
            else
            {
                UpdateAssetBundleFinish();
            }
        }

        private static IEnumerator DownloadAssetBundleZip(Queue<UpdateFileInfo> updateFiles)
        {
            currentDownloadFile = updateFiles.Dequeue();
            long fileSize = currentDownloadFile.fileSize;
            string fileName = currentDownloadFile.fileName;
            string serverFilePath = Path.Combine(serverABDirectory, fileName);
            string localFilePath = Path.Combine(localABDirectory, fileName);
            logger.I("开始下载文件：" + currentDownloadFile.fileName);

            long size = 0;
            using (currentWebRequest = UnityWebRequest.Get(serverFilePath))
            {
                yield return currentWebRequest.SendWebRequest();
                if (string.IsNullOrEmpty(currentWebRequest.error))
                {
                    size = currentWebRequest.downloadHandler.data.Length;
                    DownloadedSize += size;
                    CurrentProgress = DownloadedSize / TotalSize;
                    if (size > 0)
                    {
                        File.WriteAllBytes(localFilePath, currentWebRequest.downloadHandler.data);
                        if (Path.GetExtension(localFilePath) == ".zip")
                        {
                            using (ZipArchive zipArchive = ZipFile.OpenRead(localFilePath))
                            {
                                foreach (ZipArchiveEntry item in zipArchive.Entries)
                                {
                                    string path = Path.Combine(localABDirectory, item.FullName);
                                    item.ExtractToFile(path, true);
                                }
                            }
                        }
                        logger.I(string.Format("更新AssetBundle文件:{0}成功 ", fileName));
                    }
                    else
                    {
                        logger.I("下载文件为空" + fileName);
                    }
                }
                else
                {
                    logger.I(string.Format("资源{0}下载出错：{1},加入错误队列,准备重新下载", fileName, currentWebRequest.error));
                    downloadedErrorFiles.Enqueue(currentDownloadFile);
                }

                //下载完成
                if (willDownloadFiles.Count <= 0 && downloadedErrorFiles.Count <= 0)
                {
                    logger.I("6.所有热更文件下载完毕,热更准备结束");
                    Event_DownloadFinished?.Invoke();
                    UpdateAssetBundleFinished(true);
                }
                else
                {
                    //未下载完成
                    if (willDownloadFiles.Count > 0)
                    {
                        CoroutineUtil.DoCoroutine(DownloadAssetBundleZip(willDownloadFiles));
                    }
                    //下载存在错误
                    else if (downloadedErrorFiles.Count > 0)
                    {
                        errorCount++;
                        //重新下载错误的文件

                        if (errorCount > 3)
                        {
                            logger.I("存在下载失败的文件，且重试超过三次");
                            Event_DownloadError?.Invoke();
                        }
                        else
                        {
                            logger.I("所有文件都下载过一遍，存在错误文件，准备重新下载");
                            CoroutineUtil.DoCoroutine(DownloadAssetBundleZip(downloadedErrorFiles));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 资源热更环节结束
        /// </summary>
        /// <param name="haveNewResource"></param>
        private static void UpdateAssetBundleFinished(bool haveNewResource)
        {
            //替换掉本地MD5文件
            if (haveNewResource)
            {
                logger.I("将服务器版本文件拷贝到本地");
                File.WriteAllText(localVersionPath, serverVersionInfo);
            }
            logger.I("热更环节结束");
            Event_HotPatchFinished?.Invoke();
        }

        /// <summary>
        /// 获取压缩包的版本信息
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <returns></returns>
        private static Dictionary<string, UpdateFileInfo> GetZipFileVersionInfo(string[] versionInfo)
        {
            Dictionary<string, UpdateFileInfo> result = new Dictionary<string, UpdateFileInfo>();
            for (int i = 1; i < versionInfo.Length; i++)
            {
                string[] lineContent = versionInfo[i].Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                string fileName = lineContent[0];
                if (fileName.EndsWith(".zip"))
                {
                    UpdateFileInfo info = new UpdateFileInfo
                    {
                        fileName = lineContent[0],
                        fileHash = lineContent[1],
                        fileSize = Convert.ToInt64(lineContent[2])
                    };

                    result.Add(fileName, info);
                }
                else
                {
                    if (uncompressedFile.Contains(fileName))
                    {
                        UpdateFileInfo info = new UpdateFileInfo
                        {
                            fileName = lineContent[0],
                            fileHash = lineContent[1],
                            fileSize = Convert.ToInt64(lineContent[2])
                        };
                        result.Add(fileName, info);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 获取非压缩包的版本信息
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <returns></returns>
        public static Dictionary<string, UpdateFileInfo> GetSourceFileVersionInfo(string[] versionInfo)
        {
            StringBuilder log = new StringBuilder();
            Dictionary<string, UpdateFileInfo> result = new Dictionary<string, UpdateFileInfo>();
            for (int i = 1; i < versionInfo.Length; i++)
            {
                string[] lineContent = versionInfo[i].Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                string fileName = lineContent[0];
                string md5 = lineContent[1];
                if (fileName.EndsWith(".zip") == false)
                {
                    UpdateFileInfo info = new UpdateFileInfo
                    {
                        fileName = lineContent[0],
                        fileHash = lineContent[1],
                        fileSize = Convert.ToInt64(lineContent[2])
                    };
                    log.AppendLine(string.Format("{0}|{1}|{2}", info.fileName, info.fileHash, info.fileSize));
                    result.Add(fileName, info);
                }
            }
            return result;
        }

        private static void Clear()
        {
            errorCount = 0;
            CurrentPhase = Phase.None;
            willDownloadFiles.Clear();
            downloadedErrorFiles.Clear();
        }

        #endregion
    }
}