using UnityEngine;
using System;
using System.Collections;

namespace GameFramework
{
    /// <summary>
    /// 协同工具类，在代码中的任意地方开启Unity3D的协同
    /// </summary>
    public class CoroutineUtil : MonoBehaviour
    {
        private static CoroutineUtil instance;

        IEnumerator Perform(IEnumerator coroutine, Action callback)
        {
            yield return StartCoroutine(coroutine);
            callback?.Invoke();
        }

        /// <summary>
        /// 开始一个协同
        /// </summary>
        /// <param name="coroutine">协同函数</param>
        /// <param name="callback">协同完成回调函数</param>
        public static void DoCoroutine(IEnumerator coroutine, Action callback = null)
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(CoroutineUtil)) as CoroutineUtil;
                if (instance == null)
                {
                    instance = new GameObject("CoroutineTool").AddComponent<CoroutineUtil>();
                }
                DontDestroyOnLoad(instance);
            }
            instance.StartCoroutine(instance.Perform(coroutine, callback));
        }

        public static void DoStopCoroutine(IEnumerator coroutine, Action callback = null)
        {
            if (instance != null)
            {
                instance.StopCoroutine(instance.Perform(coroutine, callback));
            }
        }

        /// <summary>
        /// 停止所有协同操作
        /// </summary>
        public static void DoStopAllCoroutine()
        {
            if (instance != null)
            {
                instance.StopAllCoroutines();
            }
        }
    }
}