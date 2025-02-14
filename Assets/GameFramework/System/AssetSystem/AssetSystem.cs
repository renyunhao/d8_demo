using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 封装了Resources目录与AssetBundle的资源加载管理类
    /// </summary>
    [ExecuteInEditMode]
    public static partial class AssetSystem
    {
        private static readonly string Directory_AssetBundle = "AssetBundle";
        private static readonly string File_AssetBundle = "AssetBundle";
        private static readonly string File_AssetBundleIni = "assetBundleIni.txt";
        private static readonly string File_ResourcesConfig = "ResourcesConfig";

        /// <summary>
        /// 所有Resources目录下的资源的数据，格式为：资源名(无扩展名)|资源类型|资源路径
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> resourcesInfoDic = new Dictionary<string, Dictionary<string, string>>();
        /// <summary>
        /// 所有生成的AssetBundle的数据，格式为：资源名(无扩展名)|资源类型|资源AssetBundleName
        /// </summary>
        private static Dictionary<string, Dictionary<string, AssetBundleInfo>> assetBundleInfoDic = new Dictionary<string, Dictionary<string, AssetBundleInfo>>();
        /// <summary>
        /// 资源缓存
        /// </summary>
        private static Dictionary<string, Dictionary<string, UnityEngine.Object>> cachedAssetsDic = new Dictionary<string, Dictionary<string, UnityEngine.Object>>();
        /// <summary>
        /// AssetBundle缓存
        /// </summary>
        private static Dictionary<string, AssetBundle> cachedAssetBundleDic = new Dictionary<string, AssetBundle>();
        /// <summary>
        /// 记录打开某个UI界面带来的资源加载，用于UI关闭时将相应的资源卸载掉来保证内存不会占用太多，进而导致应用闪退
        /// </summary>
        private static Dictionary<string, List<AssetBundle>> uiAssetBundleDict = new Dictionary<string, List<AssetBundle>>();
        /// <summary>
        /// AssetBundle依赖关系汇总文件
        /// </summary>
        private static AssetBundleManifest assetBundleDependencySummary = null;
        private static AssetBundle summaryBundle = null;
        private static string recordingUI;
        private static string local_ABDirectory;

        /// <summary>
        /// 正在进行的加载器
        /// </summary>
        private static List<AssetBundleLoader> runningLoaders = new List<AssetBundleLoader>();

        /// <summary>
        /// 由于资源异步加载，当一资源有多个同时加载调用时，只有第一个请求去真正加载资源，其他请求需要等待第一个加载完成后再直接执行回调
        /// </summary>
        private static List<AssetBundleLoader> waitingLoaders = new List<AssetBundleLoader>();

        #region Public Methods

        /// <summary>
        /// ResMgr初始化:资源加载模块初始化调用的必要条件是本地资源初始化完成，同时应该只调用一次
        /// 同时初始化只会将resini、AssetBundle依赖文件、AssetBundleIni文件读取到内存中
        /// </summary>
        public static void Initialize()
        {
            local_ABDirectory = Path.Combine(Application.persistentDataPath, Directory_AssetBundle);
            LoadResourcesDependentFile();
            LoadAssetBundleDependentFile();

            //UISystem.Event_UIWillShow += UISystem_Event_UIWillShow;
            //UISystem.Event_UIShow += UISystem_Event_UIShow;
        }

        //private static void UISystem_Event_UIWillShow(BaseUI ui)
        //{
        //    StartRecordUIAsset(ui.Name);
        //}

        //private static void UISystem_Event_UIShow(BaseUI ui)
        //{
        //    StopRecordUIAsset();
        //}

        /// <summary>
        /// 重新初始化
        /// 由于热更的存在，会发生AssetBundleIni文件和AssetBundle依赖文件发生变更，需要重新初始化
        /// </summary>
        public static void ReInitialize()
        {
            //重新加载前一定要卸载
            //这两个资源不在mCachedAssetBundle中，所以单独卸载
            if (summaryBundle != null)
            {
                summaryBundle.Unload(true);
            }
            if (assetBundleDependencySummary != null)
            {
                assetBundleDependencySummary = null;
            }
            foreach (var item in cachedAssetBundleDic)
            {
                item.Value.Unload(true);
            }
            assetBundleInfoDic.Clear();
            cachedAssetBundleDic.Clear();
            cachedAssetsDic.Clear();
            uiAssetBundleDict.Clear();

            Resources.UnloadUnusedAssets();
            GC.Collect();

            LoadAssetBundleDependentFile();
        }

        /// <summary>
        /// 获取指定目录下的所有资源
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAssetsByDirectoryPath(string directoryPath)
        {
            List<string> result = new List<string>();
            foreach (var typeDict in resourcesInfoDic.Values)
            {
                foreach (var kvp in typeDict)
                {
                    if (kvp.Value == directoryPath)
                    {
                        result.Add(kvp.Key);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <returns></returns>
        public static T Load<T>(string assetName) where T : UnityEngine.Object
        {
            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                return LoadGameObject<GameObject>(assetName, nameof(GameObject))?.GetComponent<T>();
            }
            else
            {
                return LoadGameObject<T>(assetName, typeof(T).Name);
            }

            static T1 LoadGameObject<T1>(string assetName, string type) where T1 : UnityEngine.Object
            {
                if (assetBundleInfoDic.TryGetValue(assetName, out var bundleDict) && bundleDict.ContainsKey(type))
                {
                    return LoadAssetFromAssetBundle<T1>(assetName);
                }
                else if (resourcesInfoDic.TryGetValue(assetName, out var pathDict) && pathDict.ContainsKey(type))
                {
                    return LoadAssetFromResource<T1>(assetName);
                }
                else
                {
                    Debug.LogError("你要加载的资源不存在 资源名称：" + assetName);
                    return null;
                }
            }
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <returns></returns>
        public static void LoadAsync<T>(string assetName, Action<T> loadCallback) where T : UnityEngine.Object
        {
            string mainAsset = assetName;
            if (mainAsset.Contains("/"))
            {
                mainAsset = mainAsset.Split('/')[0];
            }
            if (assetBundleInfoDic.ContainsKey(mainAsset))
            {
                //资源不在[已加载完成，正在加载，等待加载]的范围时，执行异步资源加载动作
                if (IsAssetShouldLoad<T>(assetName, (res) => loadCallback?.Invoke(res as T)))
                {
                    LoadFromAssetBundleAsync<T>(assetName, loadCallback);
                }
            }
            else if (resourcesInfoDic.ContainsKey(mainAsset))
            {
                T asset = LoadFromResource<T>(assetName);
                loadCallback?.Invoke(asset);
            }
            else
            {
                loadCallback?.Invoke(null);
                Debug.LogError("你要加载的资源不存在 资源名称：" + assetName);
            }
        }

        public static T LoadFromResource<T>(string assetName) where T : UnityEngine.Object
        {
            string mainAsset = assetName;
            if (resourcesInfoDic.ContainsKey(mainAsset))
            {
                return LoadAssetFromResource<T>(assetName);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 从AssetBundle中同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static T LoadFromAssetBundle<T>(string assetName) where T : UnityEngine.Object
        {
            if (assetBundleInfoDic.ContainsKey(assetName))
            {
                return LoadAssetFromAssetBundle<T>(assetName);
            }
            else
            {
                return null;
            }
        }

        public static bool Have(string assetName)
        {
            bool result = false;
            if (assetBundleInfoDic.ContainsKey(assetName))
            {
                result = true;
            }
            else if (resourcesInfoDic.ContainsKey(assetName))
            {
                result = true;
            }
            return result;
        }

        /// <summary>
        /// 从AssetBundle中异步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private static void LoadFromAssetBundleAsync<T>(string assetName, Action<T> loadCallback) where T : UnityEngine.Object
        {
            AssetBundleInfo info = assetBundleInfoDic[assetName][typeof(T).Name];

            //由于一个AssetBundle中可能包含不同的资源，因此加载不同资源的时候同，要先检查当前资源所在的AssetBundle是否已经被加载过了
            if (IsAssetBundleShouldLoad(info.assetBundleName, out AssetBundle mainAssetBundle, out _))
            {
                AssetBundleLoader loader = new AssetBundleLoader(info.assetBundleName, GetAssetBundleFullName(info.assetBundleName));
                loader.AddAssetRecord(assetName, typeof(T), asset => loadCallback?.Invoke(asset as T));
                string[] dependencies = assetBundleDependencySummary.GetAllDependencies(info.assetBundleName);
                foreach (string dependency in dependencies)
                {
                    //异步方法加载AssetBundle，检查是否加载过时，需要判断的内容较多，具体见方法说明
                    if (IsAssetBundleShouldLoad(dependency, out _, out AssetBundleLoader dependencyLoader))
                    {
                        LoadAssetBundleAsync(dependency, loader);
                    }
                    else
                    {
                        //不需要加载的AssetBundle也可能是某个资源的依赖项，这个依赖关系得记录
                        if (dependencyLoader != null)
                        {
                            loader.AddDepend(dependencyLoader);
                        }
                    }
                }
                waitingLoaders.Add(loader);
            }
            else
            {
                //当不需要加载该资源所在的AssetBundle时，可能是已经加载过了，也可能还等待加载
                //对于已经加载过了，直接执行加载该资源即可
                if (mainAssetBundle != null)
                {
                    LoadAssetFromAssetBundleAsync(assetName, typeof(T), mainAssetBundle, asset => loadCallback?.Invoke(asset as T));
                }
            }
        }

        #region 异步改动

        /// <summary>
        /// AssetBundleName与相应的AssetBundleLoader对照字典
        /// </summary>
        private static Dictionary<string, AssetBundleLoader> assetBundleLoaderDic = new Dictionary<string, AssetBundleLoader>();

        /// <summary>
        /// 20230222新增方法：异步加载资源
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <returns></returns>
        public static async UniTask<T> LoadAsync<T>(string assetName) where T : UnityEngine.Object
        {
            //Debug.Log($"加载资源：{assetName}");
            string mainAsset = assetName;
            if (mainAsset.Contains("/"))
            {
                mainAsset = mainAsset.Split('/')[0];
            }
            if (assetBundleInfoDic.ContainsKey(mainAsset))
            {
                //资源不在[已加载完成，正在加载，等待加载]的范围时，执行异步资源加载动作
                if (IsAssetShouldLoad<T>(assetName, out UnityEngine.Object outAsset))
                {
                    var asset = await LoadFromAssetBundleAsync<T>(assetName);
                    return asset;
                }
                else
                {
                    return (T)outAsset;
                }
            }
            else if (resourcesInfoDic.ContainsKey(mainAsset))
            {
                T asset = LoadFromResource<T>(assetName);
                return asset;
            }
            else
            {
                Debug.LogError("你要加载的资源不存在 资源名称：" + assetName);
                return null;
            }
        }

        /// <summary>
        /// 20230222新增方法：从AssetBundle中异步加载资源
        /// </summary>
        private static async UniTask<T> LoadFromAssetBundleAsync<T>(string assetName) where T : UnityEngine.Object
        {
            AssetBundleInfo info = assetBundleInfoDic[assetName][typeof(T).Name];

            //检查是否存有对应的loader存在,存在则取回，不存在则添加
            AssetBundleLoader loader = GetAssetBundleLoader(info.assetBundleName);

            await loader.DoLoadAsync();

            var asset = LoadAssetFromAssetBundleDirectly(assetName, typeof(T), loader.AssetBundle);

            return (T)asset;
        }

        /// <summary>
        /// 20230222新增方法：异步加载[assetBundleName]所依赖的AssetBundle
        /// </summary>
        private static async void LoadDependencyAssetBundleAsync(string assetBundleName)
        {
            string[] dependencies = assetBundleDependencySummary.GetAllDependencies(assetBundleName);
            foreach (string dependency in dependencies)
            {
                if (IsAssetBundleShouldLoad(dependency))
                {
                    AssetBundleLoader loader = GetAssetBundleLoader(assetBundleName);

                    //循环检查依赖
                    LoadDependencyAssetBundleAsync(loader.AssetBundleName);

                    //加载当前依赖
                    await loader.DoLoadAsync();
                }
            }
        }

        /// <summary>
        /// 获取AssetBundleLoader对象，存在时直接返回，不存在时创建一个新的
        /// </summary>
        private static AssetBundleLoader GetAssetBundleLoader(string assetBundleName)
        {
            if (assetBundleLoaderDic.ContainsKey(assetBundleName))
            {
                var loader = assetBundleLoaderDic[assetBundleName];
                return loader;
            }
            else
            {
                var loader = new AssetBundleLoader(assetBundleName, GetAssetBundleFullName(assetBundleName));
                assetBundleLoaderDic.Add(assetBundleName, loader);
                return loader;
            }
        }

        /// <summary>
        /// 检查要加载的资源是否已经加载完成，或者正在加载，或者等待加载
        /// 是以上三种情形之一，返回false，否则返回true
        /// </summary>
        /// <param name="assetName">要加载的资源</param>
        private static bool IsAssetShouldLoad<T>(string assetName, out UnityEngine.Object outAsset) where T : UnityEngine.Object
        {
            outAsset = null;

            //如果缓存里有，直接返回
            if (cachedAssetsDic.TryGetValue(assetName, out var dict))
            {
                if (dict.TryGetValue(typeof(T).Name, out var asset))
                {
                    outAsset = asset;
                }
                return false;
            }

            //不存在loader，需要加载
            return true;
        }

        /// <summary>
        /// 20230222新增方法：检查要加载的AssetBundle是否已经加载完成，或者等待加载，或者正在加载
        /// 是以上三种情形之一，返回false(表示不需要加载资源)，否则返回true（表示需要再加载资源）
        /// </summary>
        private static bool IsAssetBundleShouldLoad(string assetBundleName)
        {
            //bundle资源已加载完成并缓存，则直接取回，不需要再次加载
            if (cachedAssetBundleDic.ContainsKey(assetBundleName))
            {
                return false;
            }

            //不存在loader，需要加载
            return true;
        }

        /// <summary>
        /// 20230222新增方法：直接从AssetBundle中加载资源
        /// </summary>
        /// <returns></returns>
        private static UnityEngine.Object LoadAssetFromAssetBundleDirectly(string assetName, Type assetType, AssetBundle assetBundle)
        {
            bool isLoadingComponent = assetType.IsSubclassOf(typeof(Component));
            UnityEngine.Object asset = null;
            if (isLoadingComponent)
            {
                asset = assetBundle.LoadAsset<GameObject>(assetName).GetComponent(assetType);
            }
            else
            {
                asset = assetBundle.LoadAsset(assetName, assetType);
            }
            if (asset != null)
            {
                AddAssetToCache(assetName, assetType.Name, asset);
            }

            return asset;
        }

        #endregion

        /// <summary>
        /// 卸载Asset资源（Resources目录下的资源），调用的时候确保没有其他对象引用要卸载的资源了
        /// </summary>
        /// <param name="asset">要卸载的资源</param>
        public static void UnLoadAsset(UnityEngine.Object asset, bool forceTriggerGC = false)
        {
            if (asset != null)
            {
                Resources.UnloadAsset(asset);
                Resources.UnloadUnusedAssets();
            }
            if (forceTriggerGC)
            {
                GC.Collect();
            }
        }

        public static bool CheckAssetbundleContains(string assetBundleName)
        {
            if (cachedAssetBundleDic.ContainsKey(assetBundleName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 完全卸载AssetBundle资源
        /// </summary>
        /// <param name="assetBundleName">AssetBundle的名字</param>
        public static void UnloadAssetBundle(string assetBundleName)
        {
            if (cachedAssetBundleDic.ContainsKey(assetBundleName))
            {
                cachedAssetBundleDic[assetBundleName].Unload(true);
                cachedAssetBundleDic.Remove(assetBundleName);
            }
            else
            {
                Debug.LogError("你要卸载的资源不归ResMgr管或尚未加载");
            }
        }

        /// <summary>
        /// 完全卸载AssetBundle资源
        /// </summary>
        /// <param name="assetBundleName">AssetBundle的名字</param>
        public static void UnloadAssetBundle(AssetBundle assetBundle)
        {
            if (cachedAssetBundleDic.ContainsValue(assetBundle))
            {
                //因为完全卸载后，对象就变成null了，在下次加载时，会处理null情况，于是在这里就不遍历字典查找key,然后删除该项了
                assetBundle.Unload(true);
            }
            else
            {
                //你要卸载的资源不归ResMgr管，请自行卸载
                Debug.LogError("(编辑器中请忽略)你要卸载的资源不归ResMgr管，请自行卸载");
            }
        }

        /// <summary>
        /// 卸载AssetBundle的内存镜像
        /// 注意，不会完全卸载
        /// 虽然不会完全卸载，但是对象仍然会变成null，然而下次加载的时候会报重复错误，使用者需要小心合理使用
        /// </summary>
        public static void UnloadAssetBundleMemoryImage(string assetBundleName)
        {
            if (cachedAssetBundleDic.ContainsKey(assetBundleName))
            {
                cachedAssetBundleDic[assetBundleName].Unload(false);
                cachedAssetBundleDic.Remove(assetBundleName);
            }
            else
            {
                //你要卸载的资源不归ResMgr管，请自行卸载
                Debug.LogError("你要卸载的资源不归ResMgr管，请自行卸载");
            }
        }

        /// <summary>
        /// 卸载AssetBundle的内存镜像
        /// 注意，不会完全卸载
        /// 虽然不会完全卸载，但是对象仍然会变成null，然而下次加载的时候会报重复错误，使用者需要小心合理使用
        /// </summary>
        public static void UnloadAssetBundleMemoryImage(AssetBundle assetBundle)
        {
            if (cachedAssetBundleDic.ContainsValue(assetBundle))
            {
                //因为完全卸载后，对象就变成null了，在下次加载时，会处理null情况，于是在这里就不遍历字典查找key,然后删除该项了
                assetBundle.Unload(false);
            }
            else
            {
                //你要卸载的资源不归ResMgr管，请自行卸载
                Debug.LogError("你要卸载的资源不归ResMgr管，请自行卸载");
            }
        }

        public static void StartRecordUIAsset(string uiName)
        {
            recordingUI = uiName;
            if (uiAssetBundleDict.ContainsKey(uiName) == false)
            {
                uiAssetBundleDict.Add(uiName, new List<AssetBundle>());
            }
        }

        public static void StopRecordUIAsset()
        {
            recordingUI = string.Empty;
        }

        public static void UnloadUIAsset(string uiName)
        {
            if (uiAssetBundleDict.ContainsKey(uiName))
            {
                List<AssetBundle> abs = uiAssetBundleDict[uiName];
                foreach (AssetBundle ab in abs)
                {
                    UnloadAssetBundle(ab);
                }
                abs.Clear();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 加载Resources资源配置文件
        /// </summary>
        private static void LoadResourcesDependentFile()
        {
            resourcesInfoDic.Clear();
            TextAsset txt = Resources.Load<TextAsset>(File_ResourcesConfig);
            if (txt != null)
            {
                System.IO.TextReader reader = new System.IO.StringReader(txt.text);
                string lineTxt = reader.ReadLine();
                while (!string.IsNullOrEmpty(lineTxt))
                {
                    string[] resData = lineTxt.Split('|');
                    string name = resData[0];
                    string type = resData[1];
                    string path = resData[2];
                    if (resourcesInfoDic.TryGetValue(name, out var typeDict) == false)
                    {
                        typeDict = new Dictionary<string, string>();
                        resourcesInfoDic.Add(name, typeDict);
                    }
                    typeDict.Add(type, path);
                    lineTxt = reader.ReadLine();
                }
                reader.Close();
            }
        }

        /// <summary>
        /// Update方法，目前包含的逻辑是处理所有的Loader（异步加载产生的）
        /// 特别注意！业务代码需要在游戏的主Update方法中调用此Update方法
        /// </summary>
        private static void Update()
        {
            if (runningLoaders.Count != 0)
            {
                for (int i = 0; i < runningLoaders.Count; i++)
                {
                    if (runningLoaders[i].IsDone)
                    {
                        AsyncLoadFinish(runningLoaders[i]);
                        runningLoaders.RemoveAt(i);
                        i--;
                    }
                }
            }

            for (int i = 0; i < waitingLoaders.Count; i++)
            {
                var loader = waitingLoaders[i];
                if (loader.CanStartLoad)
                {
                    runningLoaders.Add(loader);
                    loader.DoLoad();
                    waitingLoaders.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// 从Resources中同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        private static T LoadAssetFromResource<T>(string assetName) where T : UnityEngine.Object
        {
            T res = GetCachedAsset<T>(assetName) as T;
            string type = typeof(T).Name;
            if (res == null)
            {
                string fullPath = Path.Combine(resourcesInfoDic[assetName][type], assetName);
                res = Resources.Load<T>(fullPath);
                AddAssetToCache(assetName, type, res);
            }
            return res;
        }

        private static void AddAssetToCache(string assetName, string assetType, UnityEngine.Object asset)
        {
            if (cachedAssetsDic.TryGetValue(assetName, out var dict) == false)
            {
                dict = new Dictionary<string, UnityEngine.Object>();
                cachedAssetsDic.Add(assetName, dict);
            }
            dict.Add(assetType, asset);
        }

        private static void LoadAssetBundleDependentFile()
        {
            if (Directory.Exists(local_ABDirectory))
            {
                //AssetBundle依赖文件
                string assetBundleSummaryFile = Path.Combine(local_ABDirectory, File_AssetBundle);
                if (File.Exists(assetBundleSummaryFile))
                {
                    summaryBundle = AssetBundle.LoadFromFile(assetBundleSummaryFile);
                    assetBundleDependencySummary = summaryBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                }
                else
                {
                    Debug.Log("本地不存在AssetBundle依赖文件");
                }

                assetBundleInfoDic.Clear();
                string assetBundleIniPath = Path.Combine(local_ABDirectory, File_AssetBundleIni);
                if (File.Exists(assetBundleIniPath))
                {
                    string[] lines = File.ReadAllLines(assetBundleIniPath);
                    foreach (var item in lines)
                    {
                        string[] lineContent = item.Split('|');
                        string assetName = lineContent[0];
                        string assetType = lineContent[1];
                        string assetBundleName = lineContent[2];

                        if (assetBundleInfoDic.TryGetValue(assetName, out var bundleDict) == false)
                        {
                            bundleDict = new Dictionary<string, AssetBundleInfo>();
                            assetBundleInfoDic.Add(assetName, bundleDict);
                        }
                        bundleDict.Add(assetType, new AssetBundleInfo(assetType, assetBundleName));
                    }
                }
                else
                {
                    Debug.Log("本地不存在assetbundleIni文件");
                }
            }
            else
            {
                Debug.Log("persistentDataPath路径下不存在AssetBundle文件夹,认为不存在热更资源");
            }
        }

        /// <summary>
        /// 从AssetBundle中同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static T LoadAssetFromAssetBundle<T>(string assetName) where T : UnityEngine.Object
        {
            UnityEngine.Object res = GetCachedAsset<T>(assetName);
            string type = typeof(T).Name;
            if (res == null)
            {
                string mainAsset = assetName;
                if (mainAsset.Contains("/"))
                {
                    mainAsset = mainAsset.Split('/')[0];
                }
                if (assetBundleInfoDic.ContainsKey(mainAsset) == false)
                {
                    return null;
                }
                AssetBundleInfo info = assetBundleInfoDic[mainAsset][type];
                string[] dependencies = assetBundleDependencySummary.GetAllDependencies(info.assetBundleName);
                foreach (string dependency in dependencies)
                {
                    //同步方法只需要判断要加载的AssetBundle是否在缓存中即可，在即已经加载过了
                    AssetBundle dependAssetBundle = GetCachedAssetBundle(dependency);
                    if (dependAssetBundle == null)
                    {
                        LoadAssetBundle(dependency);
                    }
                }

                AssetBundle assetBundle = GetCachedAssetBundle(info.assetBundleName);
                if (assetBundle == null)
                {
                    string fullName = GetAssetBundleFullName(info.assetBundleName);
                    assetBundle = AssetBundle.LoadFromFile(fullName);
                    cachedAssetBundleDic.Add(info.assetBundleName, assetBundle);
                    if (string.IsNullOrEmpty(recordingUI) == false)
                    {
                        if (uiAssetBundleDict.ContainsKey(recordingUI))
                        {
                            uiAssetBundleDict[recordingUI].Add(assetBundle);
                        }
                    }
                }

                if (assetBundle != null)
                {
                    res = assetBundle.LoadAsset(assetName, typeof(T));
                    AddAssetToCache(assetName, type, res);
                }
                else
                {
                    Debug.LogErrorFormat("加载AssetBundle:{0}出错", info.assetBundleName);
                }
            }
            return (T)res;
        }

        /// <summary>
        /// 从给定的AssetBundle中加载资源（供异步加载流程使用的，当异步加载流程将一个AssetBundle异步加载完成后，执行此步）
        /// </summary>
        /// <returns></returns>
        private static void LoadAssetFromAssetBundle(string assetName, Type assetType, AssetBundle assetBundle, List<Action<UnityEngine.Object>> callbackList)
        {
            bool isLoadingComponent = assetType.IsSubclassOf(typeof(Component));
            UnityEngine.Object asset = null;
            if (isLoadingComponent)
            {
                asset = assetBundle.LoadAsset<GameObject>(assetName).GetComponent(assetType);
            }
            else
            {
                asset = assetBundle.LoadAsset(assetName, assetType);
            }
            if (asset != null)
            {
                AddAssetToCache(assetName, assetType.Name, asset);
            }
            for (int i = 0; i < callbackList.Count; i++)
            {
                if (callbackList[i] != null)
                {
                    callbackList[i](asset);
                }
            }
        }

        /// <summary>
        /// 从AssetBundle中异步加载资源
        /// </summary>
        /// <returns></returns>
        private static void LoadAssetFromAssetBundleAsync(string assetName, Type assetType, AssetBundle assetBundle, Action<UnityEngine.Object> callback)
        {
            bool isLoadingComponent = assetType.IsSubclassOf(typeof(Component));
            UnityEngine.Object asset = null;
            if (isLoadingComponent)
            {
                asset = assetBundle.LoadAsset<GameObject>(assetName).GetComponent(assetType);
            }
            else
            {
                asset = assetBundle.LoadAsset(assetName, assetType);
            }
            if (asset != null)
            {
                AddAssetToCache(assetName, assetType.Name, asset);
            }
            callback?.Invoke(asset);
        }

        /// <summary>
        /// 同步加载AssetBundle
        /// </summary>
        /// <param name="assetBundleName"></param>
        private static void LoadAssetBundle(string assetBundleName)
        {
            string[] dependencies = assetBundleDependencySummary.GetAllDependencies(assetBundleName);
            foreach (string dependency in dependencies)
            {
                //同步方法只需要判断要加载的AssetBundle是否在缓存中即可，在即已经加载过了
                AssetBundle dependAssetBundle = GetCachedAssetBundle(dependency);
                if (dependAssetBundle == null)
                {
                    LoadAssetBundle(dependency);
                }
            }

            string fullName = GetAssetBundleFullName(assetBundleName);
            AssetBundle assetBundle = AssetBundle.LoadFromFile(fullName);
            cachedAssetBundleDic.Add(assetBundleName, assetBundle);
            if (string.IsNullOrEmpty(recordingUI) == false)
            {
                if (uiAssetBundleDict.ContainsKey(recordingUI))
                {
                    uiAssetBundleDict[recordingUI].Add(assetBundle);
                }
            }
        }

        /// <summary>
        /// 异步加载AssetBundle
        /// </summary>
        /// <param name="assetBundleName">要加载的AssetBundle</param>
        /// <param name="startLoader">如果该AssetBundle是一个被依赖项，那么startLoader是依赖它的那个加载请求</param>
        private static void LoadAssetBundleAsync(string assetBundleName, AssetBundleLoader startLoader)
        {
            AssetBundleLoader loader = new AssetBundleLoader(assetBundleName, GetAssetBundleFullName(assetBundleName));
            startLoader.AddDepend(loader);
            string[] dependencies = assetBundleDependencySummary.GetAllDependencies(assetBundleName);
            foreach (string dependency in dependencies)
            {
                if (IsAssetBundleShouldLoad(dependency, out _, out AssetBundleLoader dependencyLoader))
                {
                    LoadAssetBundleAsync(dependency, loader);
                }
                else
                {
                    //不需要加载的AssetBundle也可能是某个资源的依赖项，这个依赖关系得记录
                    if (dependencyLoader != null)
                    {
                        loader.AddDepend(dependencyLoader);
                    }
                }
            }
            waitingLoaders.Add(loader);
        }

        /// <summary>
        /// 获取AssetBundle文件的完整路径
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        private static string GetAssetBundleFullName(string assetBundleName)
        {
            string basePath = Path.Combine(Application.persistentDataPath, File_AssetBundle);
            return Path.Combine(basePath, assetBundleName);
        }

        /// <summary>
        /// 获取缓存中的资源
        /// </summary>
        /// <param name="assetName">资源名</param>
        /// <returns></returns>
        private static UnityEngine.Object GetCachedAsset<T>(string assetName) where T : UnityEngine.Object
        {
            if (cachedAssetsDic.TryGetValue(assetName, out var dict))
            {
                string type = typeof(T).Name;
                if (dict.TryGetValue(type, out var asset))
                {
                    if (asset == null)
                    {
                        dict.Remove(type);
                    }
                }
                return asset;
            }
            return null;
        }

        /// <summary>
        /// 获取缓存的AssetBundle
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        private static AssetBundle GetCachedAssetBundle(string assetBundleName)
        {
            if (cachedAssetBundleDic.ContainsKey(assetBundleName))
            {
                AssetBundle assetBundle = cachedAssetBundleDic[assetBundleName];
                if (assetBundle == null)
                {
                    cachedAssetBundleDic.Remove(assetBundleName);
                }
                return assetBundle;
            }
            return null;
        }

        /// <summary>
        /// 检查要加载的资源是否已经加载完成，或者正在加载，或者等待加载
        /// 是以上三种情形之一，返回false，否则返回true
        /// </summary>
        /// <param name="assetName">要加载的资源</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        private static bool IsAssetShouldLoad<T>(string assetName, Action<UnityEngine.Object> callback) where T : UnityEngine.Object
        {
            //如果缓存里有，直接返回
            if (cachedAssetsDic.ContainsKey(assetName))
            {
                if (callback != null)
                {
                    callback(GetCachedAsset<T>(assetName));
                }
                return false;
            }
            AssetBundleInfo info = assetBundleInfoDic[assetName][typeof(T).Name];

            //检查该资源是否正在加载中，如果是：直接将回调加入回调列表中
            foreach (AssetBundleLoader loader in runningLoaders)
            {
                if (loader.AssetBundleName == info.assetBundleName)
                {
                    loader.AddAssetRecord(assetName, typeof(T), callback);
                    return false;
                }
            }

            //检查该资源是否正在等待加载，如果是：直接将回调加入回调列表中
            foreach (AssetBundleLoader loader in waitingLoaders)
            {
                if (loader.AssetBundleName == info.assetBundleName)
                {
                    loader.AddAssetRecord(assetName, typeof(T), callback);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查要加载的AssetBundle是否已经加载完成，或者等待加载，或者正在加载
        /// 是以上三种情形之一，返回false，否则返回true
        /// </summary>
        private static bool IsAssetBundleShouldLoad(string assetBundleName, out AssetBundle assetBundle, out AssetBundleLoader existLoader)
        {
            existLoader = null;

            if (cachedAssetBundleDic.ContainsKey(assetBundleName))
            {
                assetBundle = cachedAssetBundleDic[assetBundleName];
                return false;
            }

            assetBundle = null;

            foreach (AssetBundleLoader loader in waitingLoaders)
            {
                if (loader is AssetBundleLoader)
                {
                    AssetBundleLoader abLoader = loader as AssetBundleLoader;
                    if (assetBundleName.Equals(abLoader.AssetBundleName))
                    {
                        existLoader = abLoader;
                        return false;
                    }
                }
            }

            foreach (AssetBundleLoader loader in runningLoaders)
            {
                if (loader is AssetBundleLoader)
                {
                    AssetBundleLoader abLoader = loader as AssetBundleLoader;
                    if (assetBundleName.Equals(abLoader.AssetBundleName))
                    {
                        existLoader = abLoader;
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Loader加载完成
        /// </summary>
        /// <param name="loader"></param>
        private static void AsyncLoadFinish(AssetBundleLoader loader)
        {
            AssetBundle assetBundle = loader.AssetBundle;

            if (assetBundle == null)
            {
                //未加载到bundle时，向所有回调返回null
                foreach (var assetRecord in loader.AssetRecords.Values)
                {
                    assetRecord.TriggerCallbacks(null);
                }
            }
            else
            {
                cachedAssetBundleDic.Add(loader.AssetBundleName, assetBundle);
                if (string.IsNullOrEmpty(recordingUI) == false)
                {
                    if (uiAssetBundleDict.ContainsKey(recordingUI))
                    {
                        uiAssetBundleDict[recordingUI].Add(assetBundle);
                    }
                }

                foreach (var assetRecord in loader.AssetRecords.Values)
                {
                    LoadAssetFromAssetBundle(assetRecord.AssetName, assetRecord.AssetType, loader.AssetBundle, assetRecord.Callbacks);
                }
            }
        }

        #endregion
    }
}