using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public class UpdateUtil : MonoBehaviour
    {
        private static UpdateUtil instance;

        private event Action<float> onUpdate;
        private event Action<float> onUpdateIgnoreTime;
        private event Action onLateUpdate;
        private event Action onFixedUpdate;

        private bool isRunning = true;
        //仅记录回调的名称，方便调试，无其他作用
        private Dictionary<Delegate, string> callbackNameDict = new Dictionary<Delegate, string>();

        private void Update()
        {
            if (onUpdateIgnoreTime != null)
            {
                onUpdateIgnoreTime(Time.unscaledDeltaTime);
            }
            if (onUpdate != null && isRunning)
            {
                onUpdate(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (this.onLateUpdate != null && isRunning)
            {
                this.onLateUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (this.onFixedUpdate != null && isRunning)
            {
                this.onFixedUpdate();
            }
        }

        public static UpdateUtil Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject gameObject = new GameObject("UpdateUtil");
                    DontDestroyOnLoad(gameObject);
                    instance = gameObject.AddComponent<UpdateUtil>();
                }
                return instance;
            }
        }

        public static void Pause()
        {
            Instance.isRunning = false;
        }

        public static void Resume()
        {
            Instance.isRunning = true;
        }

        public static void AddUpdate(Action<float> callback, bool ignoreTimeScale = false, string name = "")
        {
            if (ignoreTimeScale)
            {
                Instance.onUpdateIgnoreTime += callback;
            }
            else
            {
                Instance.onUpdate += callback;
            }
            Instance.callbackNameDict.Add(callback, name);
        }

        public static void RemoveUpdate(Action<float> callback)
        {
            Instance.onUpdate -= callback;
            Instance.onUpdateIgnoreTime -= callback;
            Instance.callbackNameDict.Remove(callback);
        }

        public static void AddLateUpdate(Action callback, string name = "")
        {
            Instance.onLateUpdate += callback;
            Instance.callbackNameDict.Add(callback, name);
        }

        public static void RemoveLateUpdate(Action callback)
        {
            Instance.onLateUpdate -= callback;
            Instance.callbackNameDict.Remove(callback);
        }

        public static void AddFixedUpdate(Action callback, string name = "")
        {
            Instance.onFixedUpdate += callback;
            Instance.callbackNameDict.Add(callback, name);
        }

        public static void RemoveFixedUpdate(Action callback)
        {
            Instance.onFixedUpdate -= callback;
            Instance.callbackNameDict.Remove(callback);
        }
    }
}
