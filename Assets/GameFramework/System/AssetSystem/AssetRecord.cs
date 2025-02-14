using System;
using System.Collections.Generic;

namespace GameFramework
{
    public class AssetRecord
    {
        private string mAssetName;
        private Type mAssetType;
        private List<Action<UnityEngine.Object>> mCallbacks;

        public string AssetName
        {
            get
            {
                return mAssetName;
            }
        }

        public Type AssetType
        {
            get
            {
                return mAssetType;
            }
        }

        public List<Action<UnityEngine.Object>> Callbacks
        {
            get
            {
                return mCallbacks;
            }
        }

        public AssetRecord(string assetName, Type assetType, Action<UnityEngine.Object> callback)
        {
            mAssetName = assetName;
            mAssetType = assetType;
            mCallbacks = new List<Action<UnityEngine.Object>>();
            mCallbacks.Add(callback);
        }

        public void AddCallback(Action<UnityEngine.Object> callback)
        {
            mCallbacks.Add(callback);
        }

        public void TriggerCallbacks(UnityEngine.Object asset)
        {
            foreach (var callback in mCallbacks)
            {
                callback?.Invoke(asset);
            }
        }
    }
}