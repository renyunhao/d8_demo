using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace GameFramework
{
    public static class AvatarUtil
    {
        private static readonly int AvatarCount_Memory = 100;
        private static readonly int AvatarCount_Disk = 500;

        private static readonly string Path_Local = Application.persistentDataPath + "/LocalAvatar/";
        /// <summary>
        /// 正在下载的头像列表
        /// </summary>
        private static HashSet<string> downloadingAvatar = new HashSet<string>();
        /// <summary>
        /// 头像加载完成的回调列表(一个链接对应多个加载来源)
        /// </summary>
        private static Dictionary<string, List<Action<string, Sprite>>> callbackDic = new Dictionary<string, List<Action<string, Sprite>>>();
        /// <summary>
        /// 头像缓存（最多100条）
        /// </summary>
        private static Dictionary<string, Sprite> avatarDic = new Dictionary<string, Sprite>(AvatarCount_Memory);

        public static void GetAvatar(string avatarUrl, Action<string, Sprite> callback)
        {
            if (string.IsNullOrEmpty(avatarUrl) == false)
            {
                //内存中存在
                if (avatarDic.ContainsKey(avatarUrl))
                {
                    callback?.Invoke(avatarUrl, avatarDic[avatarUrl]);
                }
                else
                {
                    //缓存中有此头像则直接使用
                    string urlMD5 = HashUtil.ComputeMD5WithString(avatarUrl);
                    if (Directory.Exists(Path_Local))
                    {
                        string filePath = Path_Local + string.Format("{0}.jpg", urlMD5);
                        if (File.Exists(filePath))
                        {
                            Sprite avatar = LoadAvatarFromLocal(urlMD5, filePath);
                            callback?.Invoke(avatarUrl, avatar);
                        }
                        else
                        {
                            JoinRefreshList(avatarUrl, urlMD5, callback);
                        }
                    }
                    //本地中都没有，，
                    else
                    {
                        JoinRefreshList(avatarUrl, urlMD5, callback);
                    }
                }
            }
        }

        public static void ClearCachedAvatar()
        {
            if (Directory.Exists(Path_Local))
            {
                Directory.Delete(Path_Local, true);
            }
        }

        /// <summary>
        /// 加入刷新列表
        /// </summary>
        private static void JoinRefreshList(string avatarUrl, string urlMD5, Action<string, Sprite> callback)
        {
            //缓存中没有此头像，首先加入刷新列表
            if (callbackDic.ContainsKey(urlMD5))
            {
                callbackDic[urlMD5].Add(callback);
            }
            else
            {
                callbackDic.Add(urlMD5, new List<Action<string, Sprite>>() { callback });
            }
            //正在下载不做处理，还没开始下载，开启协程下载头像
            if (downloadingAvatar.Contains(urlMD5) == false)
            {
                downloadingAvatar.Add(urlMD5);
                CoroutineUtil.DoCoroutine(DownloadAvatar(avatarUrl));
            }
        }

        private static IEnumerator DownloadAvatar(string avatarUrl)
        {
            Debug.Log($"下载头像：{avatarUrl}");
            string urlMD5 = HashUtil.ComputeMD5WithString(avatarUrl);
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(avatarUrl))
            {
                yield return request.SendWebRequest();
                if (request.error == null)
                {
                    Texture2D scaleTexture = ScaleTexture((request.downloadHandler as DownloadHandlerTexture).texture);
                    Sprite avatar = Sprite.Create(scaleTexture, new Rect(0, 0, scaleTexture.width, scaleTexture.height), new Vector2(0.5f, 0.5f), 100f, 1, SpriteMeshType.FullRect);
                    if (avatarDic.Count < AvatarCount_Memory)
                    {
                        avatarDic.Add(avatarUrl, avatar);
                    }
                    //启用回调
                    if (callbackDic.ContainsKey(urlMD5))
                    {
                        foreach (var item in callbackDic[urlMD5])
                        {
                            item?.Invoke(urlMD5, avatar);
                        }
                    }
                    SaveAvatarToLocal(urlMD5, scaleTexture);
                    Debug.Log($"头像下载成功：{avatarUrl}");
                }
                else
                {
                    Debug.LogError("下载头像失败，原因：" + request.error);
                }
                //无论头像是否加载到，，都需要将他从下载列表以及回调列表中移除
                downloadingAvatar.Remove(urlMD5);
                callbackDic.Remove(urlMD5);
            }
        }

        private static Sprite LoadAvatarFromLocal(string urlMD5, string filePath)
        {
            FileInfo file = new FileInfo(filePath);
            using (FileStream stream = file.OpenRead())
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                stream.Close();
                stream.Dispose();
                Texture2D texture = new Texture2D(100, 100);
                texture.LoadImage(buffer);
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            }
        }

        private static void SaveAvatarToLocal(string urlMd5, Texture2D avatar)
        {
            //创建文件夹
            if (Directory.Exists(Path_Local) == false)
            {
                Directory.CreateDirectory(Path_Local);
            }
            string[] files = Directory.GetFiles(Path_Local);
            if (files.Length > AvatarCount_Disk)
            {
                File.Delete(files[0]);
            }

            //将图片存入本地
            string filePath = Path_Local + string.Format("{0}.jpg", urlMd5);
            if (File.Exists(filePath) == false)
            {
                byte[] imageData = avatar.EncodeToJPG();
                File.WriteAllBytes(filePath, imageData);
            }
        }

        /// <summary>
        /// 缩小一个texture
        /// </summary>
        /// <param name="originTexture"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        private static Texture2D ScaleTexture(Texture2D originTexture)
        {
            int width = 128;
            int height = 128;
            int count = width * height;
            Texture2D textrue = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Color32[] originColors = originTexture.GetPixels32();
            Color32[] colors = new Color32[count];
            float x_rate = originTexture.width / (float)width;
            float y_rate = originTexture.height / (float)height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = x + y * width;
                    int j = (int)(x_rate * x) + (int)(y_rate * y) * originTexture.width;
                    colors[i] = originColors[j];
                }
            }
            textrue.SetPixels32(colors);
            textrue.Apply();
            return textrue;
        }
    }
}