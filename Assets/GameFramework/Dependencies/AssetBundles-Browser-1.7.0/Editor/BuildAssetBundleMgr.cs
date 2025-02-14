using AssetBundleBrowser.AssetBundleDataSource;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Policy;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public static class BuildAssetBundleMgr
    {
        private static readonly List<string> Special_AssetBundle_File = new List<string>()
        {
            "AssetBundle","assetBundleIni.txt",
        };
        private static readonly string Unity_Meta = "meta";
        private static readonly string File_Version = "versionInfo.txt";
        private static readonly string Path_AssetBundleZip = "../AssetBundle.zip";
        private static readonly string Address_Download_Farmat = "{0}{1}/AssetBundle/{2}";
        private static readonly string Address_Upload = "{0}/upload_zip.php?platform={1}";
        private static ABBuildInfo buildInfo;

        public static void BuildAssetBundle(ABBuildInfo info)
        {
            Debug.Log("1.开始BuildAssetBundle");
            buildInfo = info;
            if (Directory.Exists(info.outputDirectory))
            {
                Directory.Delete(info.outputDirectory, true);
            }
            Directory.CreateDirectory(info.outputDirectory);

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetBundleManifest buildManifest = BuildPipeline.BuildAssetBundles(info.outputDirectory, info.options, info.buildTarget);
            foreach (var assetBundleName in buildManifest.GetAllAssetBundles())
            {
                info.onBuild?.Invoke(assetBundleName);
            }
            Debug.Log("2.BuildAssetBundle成功,生成assetbundleini配置文件");
            GenernalAssetBundleIni();
            Debug.Log("3.删除Manifest文件");
            DeleteManifest();
            Debug.Log("4.将所有的AssetBundle独立压缩");
            List<string> deleteList = CompressAllAssetbundle();
            Debug.Log("5.生成本地版本文件");
            Dictionary<string, string> localVersion = GenernalVersionInfo();
            Debug.Log("6.将AssetBundle文件与压缩包拷贝至persistentDataPath路径下");
            string gamePath = Path.Combine(Application.persistentDataPath, "AssetBundle");
            CopyAssetBundle(gamePath);
            Debug.Log("7.删除源文件，只保留压缩文件");
            DeleteSourceFile(deleteList);
            Debug.Log(string.Format("8.读取本地参数（是否拷贝至其他目录:{0}）", info.copyPath));
            if (info.copy)
            {
                Debug.Log("9.将本地AssetBundle拷贝至：" + info.copyPath);
                CopyAssetBundle(info.copyPath);
            }
            Debug.Log(string.Format("10.读取本地参数（是否上传至服务器:{0}）", info.upload));
            if (info.upload)
            {
                Debug.Log("11.准备将本地变化的AssetBundle上传至服务器：" + info.uploadAddress);
                string platform = buildInfo.buildTarget.ToString();
                string serverHost = buildInfo.uploadAddress;
                string downloadAddress = string.Format(Address_Download_Farmat, serverHost, platform, File_Version);
                string uploadAddress = string.Format(Address_Upload, serverHost, platform);
                Debug.Log("12.压缩需要上传到服务端的AssetBundle");
                string zipName = CompressUploadAssetBundle(localVersion);
                if (localVersion.Count > 0)
                {
                    WebClient client = new WebClient();
                    client.Headers.Add("Content-Type", "binary/octet-stream");
                    try
                    {
                        byte[] result = client.UploadFile(uploadAddress, "POST", zipName);
                        string responseAsString = Encoding.Default.GetString(result);
                        Debug.Log("14.上传结束: " + responseAsString);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("14.上传服务器出现错误：" + e);
                    }
                }
                else
                {
                    Debug.Log("14.没有AssetBundle,不上传");
                }
            }
            else
            {
                Debug.Log("11.不上传assetBundle到服务器，assetbundle更新完成");
            }
        }

        /// <summary>
        /// 生成AssetBundleIni文件
        /// </summary>
        private static void GenernalAssetBundleIni()
        {
            StringBuilder content = new StringBuilder();
            foreach (var item in AssetDatabase.GetAllAssetBundleNames())
            {
                string[] assetArray = AssetDatabase.GetAssetPathsFromAssetBundle(item);
                if (assetArray.Length > 0)
                {
                    string assetPath = assetArray[0];
                    string assetName = Path.GetFileNameWithoutExtension(assetPath);
                    string fileName = Path.GetFileName(assetPath);
                    content.AppendLine(string.Format("{0}|{1}|{2}", assetName, fileName, item));
                }
                else
                {
                    Debug.Log(item + "仅有AssetbundleName,没有资源");
                }
            }
            string savePath = Path.Combine(buildInfo.outputDirectory, "assetBundleIni.txt");
            File.WriteAllText(savePath, content.ToString());
        }

        /// <summary>
        /// 删除manifest文件
        /// </summary>
        private static void DeleteManifest()
        {
            DirectoryInfo dir = new DirectoryInfo(buildInfo.outputDirectory);
            FileInfo[] files = dir.GetFiles("*.manifest.*", SearchOption.AllDirectories);
            for (var i = 0; i < files.Length; ++i)
            {
                files[i].Delete();
            }
        }

        /// <summary>
        /// 压缩Assetbundle(每个文件独立压缩)
        /// </summary>
        /// <param name="directory"></param>
        private static List<string> CompressAllAssetbundle()
        {
            List<string> deleteList = new List<string>();
            string directory = buildInfo.outputDirectory;
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            FileInfo[] fileInfos = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            for (int i = 0; i < fileInfos.Length; i++)
            {
                FileInfo current = fileInfos[i];
                string extension = Path.GetExtension(current.FullName);
                string nameWithExtension = Path.GetFileName(current.FullName);
                if (Special_AssetBundle_File.Contains(nameWithExtension) == false && extension.Equals(Unity_Meta) == false)
                {
                    deleteList.Add(current.FullName);
                    string zipPath = Path.Combine(directory, current.Name + ".zip");
                    using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        ZipArchiveEntry entry = archive.CreateEntryFromFile(current.FullName, current.Name, System.IO.Compression.CompressionLevel.Optimal);
                    }

                    //修改压缩文件时间,在计算文件MD5值时,文件的修改时间也会参与计算,为了保证在内容一致的情况下MD5值一样,需要
                    //此时间一致
                    using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                    {
                        ZipArchiveEntry entry = archive.GetEntry(current.Name);
                        entry.LastWriteTime = new DateTimeOffset(new DateTime(2019, 1, 1));
                    }
                }
            }
            return deleteList;
        }

        /// <summary>
        /// 生成版本文件,并在内存中保留数据用以在上传服务器时对比MD5进行增量更新
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private static Dictionary<string,string> GenernalVersionInfo()
        {
            string directory = buildInfo.outputDirectory;
            Dictionary<string, string> result = new Dictionary<string, string>();
            StringBuilder content = new StringBuilder();
            string date = DateTime.Now.ToString("yyyyMMddHHmmss");
            content.AppendLine(date);
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            FileInfo[] fileInfos = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
            foreach (var item in fileInfos)
            {
                string extension = Path.GetExtension(item.FullName);
                string nameWithExtension = Path.GetFileName(item.FullName);
                //排除meta文件
                if (extension.Equals(Unity_Meta) == false)
                {
                    string md5 = string.Empty;
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
                    string line = string.Format("{0}|{1}|{2}", nameWithExtension, md5, item.Length);
                    result.Add(nameWithExtension, md5);
                    content.AppendLine(line);
                }
            }
            string outPath = Path.Combine(directory, File_Version);
            File.WriteAllText(outPath, content.ToString());
            return result;
        }

        private static void DeleteSourceFile(List<string> deleteList)
        {
            for (int i = 0; i < deleteList.Count; i++)
            {
                if (File.Exists(deleteList[i]))
                {
                    File.Delete(deleteList[i]);
                }
                string metaPath = deleteList[i] + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
        }

        /// <summary>
        /// 拷贝AssetBundle到指定目录
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        private static void CopyAssetBundle(string destPath, bool delete = true)
        {
            string sourcePath = buildInfo.outputDirectory;
            if (delete)
            {
                if (Directory.Exists(destPath))
                {
                    Directory.Delete(destPath, true);
                }
                Directory.CreateDirectory(destPath);
            }
            string[] files = Directory.GetFiles(sourcePath);
            foreach (var item in files)
            {
                string fileName = Path.GetFileName(item);
                string targetPath = Path.Combine(destPath, fileName);
                File.Copy(item, targetPath, true);
            }
        }

        /// <summary>
        /// 从服务器下载版本文件
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private static Dictionary<string,string> DownloadVersion(string address)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
            request.Method = "GET";
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
            }
            catch (WebException ex)
            {
                response = ex.Response;
            }
            HttpStatusCode httpStatus = ((HttpWebResponse)response).StatusCode;
            if (httpStatus == HttpStatusCode.OK)
            {
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    string content = reader.ReadToEnd();
                    string[] lines = content.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    result = GetZipFileVersionInfo(lines);
                }
            }
            else if (httpStatus == HttpStatusCode.NotFound)
            {
                Debug.Log("根据所给地址未在资源服务器找到相应的版本文件,强制更新所有Assetbundle");
            }
            else
            {
                Debug.Log("下载资源服务器版本文件出现错误：" + httpStatus);
            }
            response.Dispose();
            response.Close();
            return result;
        }

        /// <summary>
        /// 将需要更新的内容压缩,
        /// </summary>
        private static string CompressUploadAssetBundle(Dictionary<string,string> localMd5)
        {
            string outDirectory = buildInfo.outputDirectory;
            string zipPath = Path.Combine(outDirectory,Path_AssetBundleZip);
            StringBuilder builder = new StringBuilder();
            builder.Append("13.以下文件将上传至服务器：");
            foreach (var item in localMd5)
            {
                builder.AppendLine(item.Key);
            }
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            ZipFile.CreateFromDirectory(outDirectory, zipPath, System.IO.Compression.CompressionLevel.Optimal, true);
            Debug.Log(builder.ToString());
            return zipPath;
        }

        /// <summary>
        /// 获取压缩包的版本信息
        /// </summary>
        /// <param name="versionInfo"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetZipFileVersionInfo(string[] versionInfo)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 1; i < versionInfo.Length; i++)
            {
                string[] lineContent = versionInfo[i].Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                string fileName = lineContent[0];
                if (fileName.EndsWith(".zip"))
                {
                    result.Add(fileName, lineContent[1]);
                }
                else
                {
                    if (Special_AssetBundle_File.Contains(fileName))
                    {
                        result.Add(fileName, lineContent[1]);
                    }
                }
            }
            return result;
        }
    }
}
