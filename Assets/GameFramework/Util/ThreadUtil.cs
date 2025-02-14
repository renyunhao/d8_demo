using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace GameFramework
{
    public class ThreadUtil : MonoBehaviour
    {
        private static ThreadUtil instance;
        private static ConcurrentQueue<Action> actionList = new ConcurrentQueue<Action>();

        public static void QueueActionToMainThread(Action action)
        {
            if (instance == null)
            {
                instance = new GameObject("ThreadUtil").AddComponent<ThreadUtil>();
                DontDestroyOnLoad(instance);
            }
            if (action != null)
            {
                actionList.Enqueue(action);
            }
            else
            {
                GameFramework.Debug.LogError("$子线程向主线程加入事件为空");
            }
        }

        private void Awake()
        {
            instance = this;
        }

        private void Update()
        {
            while (actionList.Count > 0)
            {
                actionList.TryDequeue(out Action action);
                if (action != null)
                {
                    action();
                }
                else
                {
                    GameFramework.Debug.LogError("子线程向主线程转出事件为空");
                }
            }
        }
    }
}