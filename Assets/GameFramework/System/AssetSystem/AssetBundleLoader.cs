using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace GameFramework
{
    /// <summary>
    /// 加载AssetBundle资源信息
    /// </summary>
    public class AssetBundleLoader
    {
        /// <summary>
        /// 加载完成后的回调
        /// </summary>
        private Dictionary<string, AssetRecord> mAssetRecords = new Dictionary<string, AssetRecord>();

        private UnityWebRequest request;

        /// <summary>
        /// 资源名称
        /// </summary>
        private string mAssetBundleName;

        /// <summary>
        /// 资源全路径
        /// </summary>
        private string mFullPath;

        private AssetBundle mAssetBundle = null;

        /// <summary>
        /// 是否已经加载完成
        /// </summary>
        private bool mIsDone = false;

        private List<AssetBundleLoader> dependList = new List<AssetBundleLoader>();

        public Dictionary<string, AssetRecord> AssetRecords
        {
            get
            {
                return mAssetRecords;
            }
        }

        public string AssetBundleName
        {
            get
            {
                return mAssetBundleName;
            }
        }

        public AssetBundle AssetBundle
        {
            get
            {
                return mAssetBundle;
            }
        }

        public virtual bool IsDone
        {
            get
            {
                return mIsDone;
            }
        }

        /// <summary>
        /// 判断当前Loader是否可以开始加载，条件是其所有依赖项都已经加载完成
        /// </summary>
        public bool CanStartLoad
        {
            get
            {
                if (dependList.Count == 0)
                {
                    return true;
                }
                else
                {
                    bool isDone = true;
                    foreach (AssetBundleLoader depend in dependList)
                    {
                        isDone &= depend.IsDone;
                        //如果有任何一个依赖项没有加载完成，直接返回false
                        if (!isDone)
                        {
                            return isDone;
                        }
                    }
                    return isDone;
                }
            }
        }

        public AssetBundleLoader(string assetBundleName, string assetBundlePath)
        {
            //GameFramework.Debug.Log($"创建AssetBundleLoader {assetBundleName}");
            mAssetBundleName = assetBundleName;
            mFullPath = assetBundlePath;
        }

        public void DoLoad()
        {
            CoroutineLoad().Forget();
        }

        public void AddDepend(AssetBundleLoader depend)
        {
            dependList.Add(depend);
        }

        private async UniTaskVoid CoroutineLoad()
        {
            request = UnityWebRequestAssetBundle.GetAssetBundle(mFullPath);
            await request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                mAssetBundle = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
            }
            else
            {
                Debug.LogError($"加载{AssetBundleName}失败：{request.error}");
            }
            mIsDone = true;
            request.Dispose();
        }

        /// <summary>
        /// 记录资产名称和对应的回调方法，待资源加载完成后统一进行回调
        /// </summary>
        /// <param name="assetName">资产名，整个项目中不可重复（有其它模块处理查重）</param>
        /// <param name="assetType"></param>
        /// <param name="callback"></param>
        public void AddAssetRecord(string assetName, Type assetType, Action<UnityEngine.Object> callback)
        {
            if (mAssetRecords.ContainsKey(assetName))
            {
                if (mAssetRecords[assetName].AssetType == assetType)
                {
                    mAssetRecords[assetName].AddCallback(callback);
                }
                else
                {
                    mAssetRecords.Add(assetName, new AssetRecord(assetName, assetType, callback));
                }
            }
            else
            {
                mAssetRecords.Add(assetName, new AssetRecord(assetName, assetType, callback));
            }
        }

        #region 异步改动
        /// <summary>
        /// 是否正在执行请求
        /// </summary>
        public bool isRequesting = false;
        /// <summary>
        /// 请求是否已完成
        /// </summary>
        public bool isReqComplete = false;
        /// <summary>
        /// 异步加载资源
        /// </summary>
        public async UniTask<AssetBundle> DoLoadAsync()
        {
            if (this.isReqComplete == true)
            {
                return this.mAssetBundle;
            }

            if (this.isRequesting == false)
            {
                this.isRequesting = true;
                this.isReqComplete = false;

                //开始请求并等待返回
                Debug.Log($"开始请求资源：{mFullPath}");
                this.request = UnityWebRequestAssetBundle.GetAssetBundle(mFullPath);
                await request.SendWebRequest();

                this.isReqComplete = true;
            }
            else
            {
                await new WaitUntil(() => this.isReqComplete == true);
            }

            if (this.request.result == UnityWebRequest.Result.Success)
            {
                this.mAssetBundle = (this.request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
            }
            else
            {
                this.mAssetBundle = null;
                Debug.LogError($"异步加载{AssetBundleName}失败：{request.error}");
            }

            return this.mAssetBundle;
        }

        #endregion
    }
}